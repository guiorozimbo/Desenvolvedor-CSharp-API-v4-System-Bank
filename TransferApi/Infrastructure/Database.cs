using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace TransferApi.Infrastructure;

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
    public static void Initialize(IDbConnectionFactory factory)
    {
        using var conn = factory.Create();
        conn.Open();

        const string createTransfer = @"CREATE TABLE IF NOT EXISTS transferencia (
idtransferencia TEXT(37) PRIMARY KEY,
idcontacorrente_origem TEXT(37) NOT NULL,
idcontacorrente_destino TEXT(37) NOT NULL,
datamovimento TEXT(25) NOT NULL,
valor REAL NOT NULL,
FOREIGN KEY(idtransferencia) REFERENCES transferencia(idtransferencia)
);";

        const string createIdem = @"CREATE TABLE IF NOT EXISTS idempotencia (
chave_idempotencia TEXT(37) PRIMARY KEY,
requisicao TEXT(1000),
resultado TEXT(1000)
);";

        conn.Execute(createTransfer);
        conn.Execute(createIdem);
    }
}


