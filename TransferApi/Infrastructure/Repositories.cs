using System.Data;
using Dapper;

namespace TransferApi.Infrastructure;

public sealed class TransferRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string OriginAccountId { get; init; } = string.Empty;
    public string DestinationAccountId { get; init; } = string.Empty;
    public DateTime Date { get; init; } = DateTime.UtcNow;
    public decimal Value { get; init; }
}

public interface ITransferRepository
{
    Task InsertAsync(TransferRecord record);
}

public interface IIdempotencyRepository
{
    Task<bool> ExistsAsync(string key);
    Task SaveAsync(string key, string? request, string? result);
}

public sealed class TransferRepository : ITransferRepository
{
    private readonly IDbConnectionFactory _factory;
    public TransferRepository(IDbConnectionFactory factory) => _factory = factory;
    public async Task InsertAsync(TransferRecord record)
    {
        const string sql = "insert into transferencia (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, datamovimento, valor) values (@Id, @OriginAccountId, @DestinationAccountId, @Date, @Value)";
        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            record.Id,
            record.OriginAccountId,
            record.DestinationAccountId,
            Date = record.Date.ToString("dd/MM/yyyy HH:mm:ss"),
            record.Value
        });
    }
}

public sealed class IdempotencyRepository : IIdempotencyRepository
{
    private readonly IDbConnectionFactory _factory;
    public IdempotencyRepository(IDbConnectionFactory factory) => _factory = factory;
    public async Task<bool> ExistsAsync(string key)
    {
        const string sql = "select count(1) from idempotencia where chave_idempotencia=@key";
        using var conn = _factory.Create();
        var count = await conn.ExecuteScalarAsync<long>(sql, new { key });
        return count > 0;
    }
    public async Task SaveAsync(string key, string? request, string? result)
    {
        const string sql = "insert into idempotencia (chave_idempotencia, requisicao, resultado) values (@key, @request, @result)";
        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new { key, request, result });
    }
}


