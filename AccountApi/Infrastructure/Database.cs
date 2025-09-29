using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AccountApi.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    public SqliteConnectionFactory(string connectionString) => _connectionString = connectionString;
    public IDbConnection Create() => new SqliteConnection(_connectionString);
}

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IDbConnectionFactory factory)
    {
        using var conn = factory.Create();
        conn.Open();

        const string createAccount = @"CREATE TABLE IF NOT EXISTS contacorrente (
idcontacorrente TEXT(37) PRIMARY KEY,
numero INTEGER(10) NOT NULL UNIQUE,
nome TEXT(100) NOT NULL,
ativo INTEGER(1) NOT NULL default 0,
senha TEXT(100) NOT NULL,
salt TEXT(100) NOT NULL,
CHECK (ativo in (0,1))
);";

        const string createMovement = @"CREATE TABLE IF NOT EXISTS movimento (
idmovimento TEXT(37) PRIMARY KEY,
idcontacorrente TEXT(37) NOT NULL,
datamovimento TEXT(25) NOT NULL,
tipomovimento TEXT(1) NOT NULL,
valor REAL NOT NULL,
CHECK (tipomovimento in ('C','D')),
FOREIGN KEY(idcontacorrente) REFERENCES contacorrente(idcontacorrente)
);";

        const string createIdempotency = @"CREATE TABLE IF NOT EXISTS idempotencia (
chave_idempotencia TEXT(37) PRIMARY KEY,
requisicao TEXT(1000),
resultado TEXT(1000)
);";

        await conn.ExecuteAsync(createAccount);
        await conn.ExecuteAsync(createMovement);
        await conn.ExecuteAsync(createIdempotency);
    }
}


