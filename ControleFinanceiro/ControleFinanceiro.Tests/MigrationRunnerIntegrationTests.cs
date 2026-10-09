using System.Data.Common;
using ControleFinanceiro.Migrations.Configuration;
using ControleFinanceiro.Migrations.Migrations;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ControleFinanceiro.Tests;

public class MigrationRunnerIntegrationTests
{
    [SqlServerFact]
    public async Task RunAsync_CreatesHistoryAndSkipsAppliedMigration()
    {
        var connectionString = RequireConnectionString();
        const string version = "9101";
        const string tableName = "migration_probe_9101";
        var directory = CreateScriptDirectory(version, tableName, "CREATE TABLE dbo.migration_probe_9101 (id INT NOT NULL);");

        try
        {
            await CleanupAsync(connectionString, version, tableName);
            var firstRun = await CreateRunner(connectionString, directory).RunAsync();
            var secondRun = await CreateRunner(connectionString, directory).RunAsync();

            Assert.Contains(version, firstRun.Applied);
            Assert.Contains(version, secondRun.Skipped);
            Assert.Equal(1, await ScalarAsync<int>(connectionString,
                $"SELECT COUNT(*) FROM dbo.__MigrationHistory WHERE MigrationVersion = '{version}';"));
        }
        finally
        {
            await CleanupAsync(connectionString, version, tableName);
            Directory.Delete(directory, recursive: true);
        }
    }

    [SqlServerFact]
    public async Task RunAsync_RollsBackFailedMigrationAndLeavesItRetryable()
    {
        var connectionString = RequireConnectionString();
        const string version = "9102";
        const string tableName = "migration_probe_9102";
        var directory = CreateScriptDirectory(version, tableName,
            "CREATE TABLE dbo.migration_probe_9102 (id INT NOT NULL); THROW 51002, 'migration failure', 1;");

        try
        {
            await CleanupAsync(connectionString, version, tableName);
            await Assert.ThrowsAnyAsync<Exception>(() => CreateRunner(connectionString, directory).RunAsync());

            Assert.Equal(0, await ScalarAsync<int>(connectionString,
                $"SELECT COUNT(*) FROM sys.tables WHERE name = '{tableName}';"));
            Assert.Equal(0, await ScalarAsync<int>(connectionString,
                $"SELECT COUNT(*) FROM dbo.__MigrationHistory WHERE MigrationVersion = '{version}';"));
        }
        finally
        {
            await CleanupAsync(connectionString, version, tableName);
            Directory.Delete(directory, recursive: true);
        }
    }

    [SqlServerFact]
    public async Task RunAsync_RejectsChangedAppliedScript()
    {
        var connectionString = RequireConnectionString();
        const string version = "9103";
        const string tableName = "migration_probe_9103";
        var directory = CreateScriptDirectory(version, tableName, "CREATE TABLE dbo.migration_probe_9103 (id INT NOT NULL);");

        try
        {
            await CleanupAsync(connectionString, version, tableName);
            await CreateRunner(connectionString, directory).RunAsync();
            File.WriteAllText(Path.Combine(directory, $"{version}_{tableName}.sql"),
                "CREATE TABLE dbo.migration_probe_9103 (id INT NOT NULL, changed BIT NOT NULL);");

            var exception = await Assert.ThrowsAsync<MigrationValidationException>(
                () => CreateRunner(connectionString, directory).RunAsync());

            Assert.Contains("changed after it was applied", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await CleanupAsync(connectionString, version, tableName);
            Directory.Delete(directory, recursive: true);
        }
    }

    [SqlServerFact]
    public async Task RunAsync_SerializesConcurrentRuns()
    {
        var connectionString = RequireConnectionString();
        const string version = "9104";
        const string tableName = "migration_probe_9104";
        var directory = CreateScriptDirectory(version, tableName,
            "WAITFOR DELAY '00:00:01'; CREATE TABLE dbo.migration_probe_9104 (id INT NOT NULL);");

        try
        {
            await CleanupAsync(connectionString, version, tableName);
            var options = new MigrationOptions(connectionString, directory, 10);
            var runs = await Task.WhenAll(
                new MigrationRunner(options).RunAsync(),
                new MigrationRunner(options).RunAsync());

            Assert.Equal(1, runs.Count(run => run.Applied.Contains(version)));
            Assert.Equal(1, runs.Count(run => run.Skipped.Contains(version)));
            Assert.Equal(1, await ScalarAsync<int>(connectionString,
                $"SELECT COUNT(*) FROM dbo.__MigrationHistory WHERE MigrationVersion = '{version}';"));
        }
        finally
        {
            await CleanupAsync(connectionString, version, tableName);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static MigrationRunner CreateRunner(string connectionString, string scriptsDirectory)
    {
        return new MigrationRunner(new MigrationOptions(connectionString, scriptsDirectory, 10));
    }

    private static string RequireConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("CONTROLE_FINANCEIRO_MIGRATIONS_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("CONTROLE_FINANCEIRO_MIGRATIONS_TEST_CONNECTION is required.");
        }

        return connectionString!;
    }

    private static string CreateScriptDirectory(string version, string tableName, string content)
    {
        var directory = Directory.CreateTempSubdirectory("controle-financeiro-migrations-integration-").FullName;
        File.WriteAllText(Path.Combine(directory, $"{version}_{tableName}.sql"), content);
        return directory;
    }

    private static async Task CleanupAsync(string connectionString, string version, string tableName)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF OBJECT_ID(N'dbo.{tableName}', N'U') IS NOT NULL DROP TABLE dbo.{tableName};
            IF OBJECT_ID(N'dbo.__MigrationHistory', N'U') IS NOT NULL
                DELETE FROM dbo.__MigrationHistory WHERE MigrationVersion = '{version}';
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)(await command.ExecuteScalarAsync())!;
    }
}

internal sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONTROLE_FINANCEIRO_MIGRATIONS_TEST_CONNECTION")))
        {
            Skip = "Set CONTROLE_FINANCEIRO_MIGRATIONS_TEST_CONNECTION to run SQL Server migration integration tests.";
        }
    }
}
