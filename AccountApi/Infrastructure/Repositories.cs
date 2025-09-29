using System.Data;
using Dapper;
using AccountApi.AccountDomain;

namespace AccountApi.Infrastructure;

public interface IAccountRepository
{
    Task<CurrentAccount?> GetByNumberAsync(long number);
    Task<CurrentAccount?> GetByIdAsync(string id);
    Task<string> InsertAsync(CurrentAccount account);
    Task DeactivateAsync(string id);
}

public interface IMovementRepository
{
    Task InsertAsync(Movement movement);
    Task<decimal> GetBalanceAsync(string accountId);
}

public interface IIdempotencyRepository
{
    Task<bool> ExistsAsync(string key);
    Task SaveAsync(IdempotencyRecord record);
}

public sealed class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _factory;
    public AccountRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<CurrentAccount?> GetByNumberAsync(long number)
    {
        const string sql = "select idcontacorrente as Id, numero as Number, nome as Name, ativo as Active, senha as PasswordHash, salt as Salt from contacorrente where numero = @number";
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<CurrentAccount>(sql, new { number });
    }

    public async Task<CurrentAccount?> GetByIdAsync(string id)
    {
        const string sql = "select idcontacorrente as Id, numero as Number, nome as Name, ativo as Active, senha as PasswordHash, salt as Salt from contacorrente where idcontacorrente = @id";
        using var conn = _factory.Create();
        return await conn.QueryFirstOrDefaultAsync<CurrentAccount>(sql, new { id });
    }

    public async Task<string> InsertAsync(CurrentAccount account)
    {
        const string sql = "insert into contacorrente (idcontacorrente, numero, nome, ativo, senha, salt) values (@Id, @Number, @Name, @Active, @PasswordHash, @Salt)";
        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, account);
        return account.Id;
    }

    public async Task DeactivateAsync(string id)
    {
        const string sql = "update contacorrente set ativo = 0 where idcontacorrente = @id";
        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new { id });
    }
}

public sealed class MovementRepository : IMovementRepository
{
    private readonly IDbConnectionFactory _factory;
    public MovementRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task InsertAsync(Movement movement)
    {
        const string sql = "insert into movimento (idmovimento, idcontacorrente, datamovimento, tipomovimento, valor) values (@Id, @AccountId, @Date, @Type, @Value)";
        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, new
        {
            movement.Id,
            AccountId = movement.AccountId,
            Date = movement.Date.ToString("dd/MM/yyyy HH:mm:ss"),
            Type = movement.Type,
            Value = movement.Value
        });
    }

    public async Task<decimal> GetBalanceAsync(string accountId)
    {
        const string sqlCredit = "select ifnull(sum(valor),0) from movimento where idcontacorrente=@accountId and tipomovimento='C'";
        const string sqlDebit = "select ifnull(sum(valor),0) from movimento where idcontacorrente=@accountId and tipomovimento='D'";
        using var conn = _factory.Create();
        var credit = await conn.ExecuteScalarAsync<decimal>(sqlCredit, new { accountId });
        var debit = await conn.ExecuteScalarAsync<decimal>(sqlDebit, new { accountId });
        return credit - debit;
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

    public async Task SaveAsync(IdempotencyRecord record)
    {
        const string sql = "insert into idempotencia (chave_idempotencia, requisicao, resultado) values (@Key, @Request, @Result)";
        using var conn = _factory.Create();
        await conn.ExecuteAsync(sql, record);
    }
}


