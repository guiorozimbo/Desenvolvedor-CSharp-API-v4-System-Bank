using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransferApi.Infrastructure;
using Shared;

namespace TransferApi.Controllers;

[ApiController]
[Route("api/transfer")]
public class TransferController : ControllerBase
{
    private readonly ITransferRepository _transfers;
    private readonly IIdempotencyRepository _idem;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    // Kafka optional

    public TransferController(ITransferRepository transfers, IIdempotencyRepository idem, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _transfers = transfers;
        _idem = idem;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public sealed record TransferRequest(string RequestId, long DestinationAccountNumber, decimal Value);

    [Authorize]
    [HttpPost]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Post(TransferRequest req)
    {
        if (await _idem.ExistsAsync(req.RequestId))
            return NoContent();
        if (req.Value <= 0)
            return BadRequest(new ErrorResponse(ErrorCodes.InvalidValue, "Valor deve ser positivo"));

        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var client = _httpClientFactory.CreateClient("account");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty));

        // debit origin (logged)
        var debit = new { RequestId = req.RequestId + ":DEBIT", Value = req.Value, Type = "D" };
        var debitResp = await client.PostAsJsonAsync("/api/account/movement", debit);
        if (!debitResp.IsSuccessStatusCode)
        {
            var problem = await debitResp.Content.ReadFromJsonAsync<ErrorResponse>();
            return BadRequest(problem ?? new ErrorResponse("ERROR", "Falha no débito"));
        }

        // credit destination
        var credit = new { RequestId = req.RequestId + ":CREDIT", Value = req.Value, Type = "C", AccountNumber = req.DestinationAccountNumber };
        var creditResp = await client.PostAsJsonAsync("/api/account/movement", credit);
        if (!creditResp.IsSuccessStatusCode)
        {
            // rollback debit
            var rollback = new { RequestId = req.RequestId + ":ROLLBACK", Value = req.Value, Type = "C" };
            await client.PostAsJsonAsync("/api/account/movement", rollback);
            var problem = await creditResp.Content.ReadFromJsonAsync<ErrorResponse>();
            return BadRequest(problem ?? new ErrorResponse("ERROR", "Falha no crédito"));
        }

        await _transfers.InsertAsync(new TransferRecord
        {
            Id = Guid.NewGuid().ToString(),
            OriginAccountId = accountId,
            DestinationAccountId = "by-number", // we do not store cpf/number; keeping ref minimal
            Date = DateTime.UtcNow,
            Value = req.Value
        });
        await _idem.SaveAsync(req.RequestId, null, null);
        // produce transfer completed for tarifas worker
        // Optionally produce to Kafka here if enabled
        return NoContent();
    }
}


