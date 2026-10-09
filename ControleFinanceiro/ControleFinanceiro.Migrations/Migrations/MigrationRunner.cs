using System.Data.Common;
using ControleFinanceiro.Migrations.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControleFinanceiro.Migrations.Migrations;

public sealed class MigrationRunner
{
    private const string HistoryTable = "dbo.__MigrationHistory";
    private const string ApplicationLockResource = "ControleFinanceiro.Database.Migrations";
    private readonly MigrationOptions _options;
    private readonly Func<DbConnection> _connectionFactory;

    public MigrationRunner(MigrationOptions options)
        : this(options, () => new SqlConnection(options.ConnectionString))
    {
    }

    public MigrationRunner(MigrationOptions options, Func<DbConnection> connectionFactory)
    {
        _options = options;
        _connectionFactory = connectionFactory;
    }

    public async Task<MigrationRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var scripts = MigrationScriptDiscovery.Discover(_options.ScriptsDirectory);
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        await EnsureHistoryTableAsync(connection, cancellationToken);

        var applied = new List<string>();
        var skipped = new List<string>();

        foreach (var script in scripts)
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await AcquireApplicationLockAsync(connection, transaction, cancellationToken);

            var history = await connection.QuerySingleOrDefaultAsync<MigrationHistoryEntry>(
                new CommandDefinition(
                    $"SELECT MigrationVersion, MigrationName, ScriptHash, AppliedAtUtc FROM {HistoryTable} WHERE MigrationVersion = @Version;",
                    new { Version = script.Version },
                    transaction,
                    cancellationToken: cancellationToken));

            if (history is not null)
            {
                if (!string.Equals(history.ScriptHash, script.Checksum, StringComparison.OrdinalIgnoreCase))
                {
                    throw new MigrationValidationException(
                        $"Migration '{script.Version}' has changed after it was applied. Restore the original script or create a new migration.");
                }

                await transaction.CommitAsync(cancellationToken);
                skipped.Add(script.Version);
                continue;
            }

            await connection.ExecuteAsync(new CommandDefinition(
                script.Content,
                transaction: transaction,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                $"INSERT INTO {HistoryTable} (MigrationVersion, MigrationName, ScriptHash, AppliedAtUtc) VALUES (@Version, @Name, @ScriptHash, SYSUTCDATETIME());",
                new
                {
                    Version = script.Version,
                    Name = script.Name,
                    ScriptHash = script.Checksum
                },
                transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            applied.Add(script.Version);
        }

        return new MigrationRunResult(applied, skipped);
    }

    private async Task EnsureHistoryTableAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await AcquireApplicationLockAsync(connection, transaction, cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition($"""
            IF OBJECT_ID(N'{HistoryTable}', N'U') IS NULL
            BEGIN
                CREATE TABLE {HistoryTable}
                (
                    MigrationVersion NVARCHAR(50) NOT NULL CONSTRAINT PK___MigrationHistory PRIMARY KEY,
                    MigrationName NVARCHAR(200) NOT NULL,
                    ScriptHash CHAR(64) NOT NULL,
                    AppliedAtUtc DATETIME2(7) NOT NULL CONSTRAINT DF___MigrationHistory_AppliedAtUtc DEFAULT (SYSUTCDATETIME())
                );
            END;
            """, transaction: transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task AcquireApplicationLockAsync(
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var result = await connection.QuerySingleAsync<int>(new CommandDefinition("""
            DECLARE @LockResult INT;
            EXEC @LockResult = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = @LockTimeoutMilliseconds;
            SELECT @LockResult;
            """,
            new
            {
                Resource = ApplicationLockResource,
                LockTimeoutMilliseconds = _options.LockTimeoutSeconds * 1000
            },
            transaction,
            cancellationToken: cancellationToken));

        if (result < 0)
        {
            throw new MigrationConcurrencyException(
                $"Could not acquire the database migration lock within {_options.LockTimeoutSeconds} seconds.");
        }
    }

    private sealed record MigrationHistoryEntry(
        string MigrationVersion,
        string MigrationName,
        string ScriptHash,
        DateTime AppliedAtUtc);
}
