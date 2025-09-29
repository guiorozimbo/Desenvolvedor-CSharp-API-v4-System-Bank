using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AccountApi.AccountDomain;
using AccountApi.Infrastructure;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Shared;

namespace AccountApi.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IAccountRepository _accounts;
    private readonly IMovementRepository _movements;
    private readonly IIdempotencyRepository _idem;
    private readonly IDbConnectionFactory _factory;
    private readonly JwtSettings _jwt;

    public AccountController(IAccountRepository accounts, IMovementRepository movements, IIdempotencyRepository idem, IDbConnectionFactory factory, JwtSettings jwt)
    {
        _accounts = accounts;
        _movements = movements;
        _idem = idem;
        _factory = factory;
        _jwt = jwt;
    }

    public sealed record SignupRequest(string Cpf, string Name, string Password);

    [HttpPost("signup")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> Signup(SignupRequest req)
    {
        if (!ValidateCpf(req.Cpf))
            return BadRequest(new ErrorResponse(ErrorCodes.InvalidDocument, "CPF inválido"));

        var salt = Guid.NewGuid().ToString("N");
        var hash = PasswordHasher.Hash(req.Password, salt);
        var number = await GenerateAccountNumberAsync();
        var account = new CurrentAccount
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Name = req.Name,
            Active = true,
            PasswordHash = hash,
            Salt = salt
        };
        await _accounts.InsertAsync(account);
        return Ok(new { accountNumber = number });
    }

    public sealed record LoginRequest(string? CpfOrAccount, string Password);

    [HttpPost("login")]
    [ProducesResponseType(typeof(JwtTokenResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        CurrentAccount? account = null;
        if (long.TryParse(req.CpfOrAccount, out var number))
        {
            account = await _accounts.GetByNumberAsync(number);
        }
        // CPF login omitted by requirement to not share sensitive data externally; we treat only number here

        if (account is null || !PasswordHasher.Verify(req.Password, account.Salt, account.PasswordHash))
            return Unauthorized(new ErrorResponse(ErrorCodes.UserUnauthorized, "Usuário ou senha inválidos"));

        var token = IssueJwt(account.Id);
        return Ok(new JwtTokenResponse(token));
    }

    [Authorize]
    [HttpPost("deactivate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Deactivate([FromBody] string password)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var account = await _accounts.GetByIdAsync(accountId);
        if (account is null)
            return BadRequest(new ErrorResponse(ErrorCodes.InvalidAccount, "Conta inválida"));
        if (!PasswordHasher.Verify(password, account.Salt, account.PasswordHash))
            return BadRequest(new ErrorResponse(ErrorCodes.UserUnauthorized, "Senha inválida"));
        await _accounts.DeactivateAsync(accountId);
        return NoContent();
    }

    public sealed record MovementRequest(string RequestId, decimal Value, string Type, long? AccountNumber = null);

    [Authorize]
    [HttpPost("movement")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Movement(MovementRequest req)
    {
        if (await _idem.ExistsAsync(req.RequestId))
            return NoContent();

        if (req.Value <= 0)
            return BadRequest(new ErrorResponse(ErrorCodes.InvalidValue, "Valor deve ser positivo"));
        if (req.Type != "C" && req.Type != "D")
            return BadRequest(new ErrorResponse(ErrorCodes.InvalidType, "Tipo inválido"));

        var accountIdFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var sourceAccount = await _accounts.GetByIdAsync(accountIdFromToken);
        if (sourceAccount is null)
            return BadRequest(new ErrorResponse(ErrorCodes.InvalidAccount, "Conta inválida"));
        if (!sourceAccount.Active)
            return BadRequest(new ErrorResponse(ErrorCodes.InactiveAccount, "Conta inativa"));

        string targetAccountId = accountIdFromToken;
        if (req.AccountNumber.HasValue)
        {
            var target = await _accounts.GetByNumberAsync(req.AccountNumber.Value);
            if (target is null)
                return BadRequest(new ErrorResponse(ErrorCodes.InvalidAccount, "Conta destino inválida"));
            if (!target.Active)
                return BadRequest(new ErrorResponse(ErrorCodes.InactiveAccount, "Conta destino inativa"));
            targetAccountId = target.Id;
            if (req.Type != "C")
                return BadRequest(new ErrorResponse(ErrorCodes.InvalidType, "Somente crédito permitido para terceiros"));
        }

        var movement = new Movement
        {
            Id = Guid.NewGuid().ToString(),
            AccountId = targetAccountId,
            Date = DateTime.UtcNow,
            Type = req.Type,
            Value = req.Value
        };
        await _movements.InsertAsync(movement);
        await _idem.SaveAsync(new IdempotencyRecord { Key = req.RequestId, Request = null, Result = null });
        return NoContent();
    }

    [Authorize]
    [HttpGet("balance")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Balance()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var account = await _accounts.GetByIdAsync(accountId);
        if (account is null)
            return BadRequest(new ErrorResponse(ErrorCodes.InvalidAccount, "Conta inválida"));
        if (!account.Active)
            return BadRequest(new ErrorResponse(ErrorCodes.InactiveAccount, "Conta inativa"));

        var balance = await _movements.GetBalanceAsync(accountId);
        return Ok(new
        {
            accountNumber = account.Number,
            accountHolder = account.Name,
            timestamp = DateTime.UtcNow,
            balance = balance
        });
    }

    private async Task<long> GenerateAccountNumberAsync()
    {
        using var conn = _factory.Create();
        conn.Open();
        // naive generator based on max + 1
        var max = await conn.ExecuteScalarAsync<long?>("select max(numero) from contacorrente");
        return (max ?? 1000000000L) + 1;
    }

    private string IssueJwt(string accountId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, accountId)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: DateTime.UtcNow.AddMinutes(_jwt.ExpirationMinutes), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool ValidateCpf(string cpf)
    {
        // Simplified validation: 11 digits
        return !string.IsNullOrWhiteSpace(cpf) && cpf.Trim().Replace(".", string.Empty).Replace("-", string.Empty).All(char.IsDigit) && cpf.Length >= 11;
    }
}


