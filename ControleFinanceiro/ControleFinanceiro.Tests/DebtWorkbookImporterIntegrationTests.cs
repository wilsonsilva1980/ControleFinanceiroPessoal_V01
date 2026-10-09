using System.Security.Cryptography;
using System.Text;
using ControleFinanceiro.DebtImport;
using ControleFinanceiro.Migrations.Configuration;
using ControleFinanceiro.Migrations.Migrations;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ControleFinanceiro.Tests;

public class DebtWorkbookImporterIntegrationTests
{
    [SqlServerFact]
    public async Task ImportAsync_IsIdempotentAndRollsBackChangedContentAtomically()
    {
        var connectionString = Environment.GetEnvironmentVariable("CONTROLE_FINANCEIRO_MIGRATIONS_TEST_CONNECTION")!;
        var scriptsDirectory = FindMigrationScriptsDirectory();
        await new MigrationRunner(new MigrationOptions(connectionString, scriptsDirectory, 300)).RunAsync();

        var description = $"Workbook importer fixture {Guid.NewGuid():N}";
        var plan = CreatePlan(description);
        var changedPlan = CreatePlan(description, secondInstallmentAmount: 21m);
        var workbookPath = Path.Combine(Path.GetTempPath(), $"debt-import-fixture-{Guid.NewGuid():N}.xlsx");
        await File.WriteAllTextAsync(workbookPath, "synthetic workbook fixture");
        var fileHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(workbookPath)));
        var importer = new DebtWorkbookImporter(connectionString);
        Guid? batchId = null;

        try
        {
            var firstImport = await importer.ImportAsync(workbookPath, plan);
            batchId = firstImport.BatchId;

            Assert.NotNull(batchId);
            Assert.Equal(1, firstImport.DebtsInserted);
            Assert.Equal(2, firstImport.InstallmentsInserted);

            var unchangedImport = await importer.ImportAsync(workbookPath, plan);
            Assert.Null(unchangedImport.BatchId);
            Assert.Equal(3, unchangedImport.AlreadyImported);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                importer.ImportAsync(workbookPath, changedPlan));
            Assert.Equal(1, await CountAsync(connectionString,
                "SELECT COUNT(*) FROM dbo.debts WHERE descricao = @Description;",
                ("Description", description)));
            Assert.Equal(2, await CountAsync(connectionString,
                """
                SELECT COUNT(*)
                FROM dbo.debt_installments installment
                INNER JOIN dbo.debts debt ON debt.id = installment.debt_id
                WHERE debt.descricao = @Description;
                """,
                ("Description", description)));
            Assert.Equal(1, await CountAsync(connectionString,
                "SELECT COUNT(*) FROM dbo.debt_import_batches WHERE source_file_hash = @FileHash;",
                ("FileHash", fileHash)));
        }
        finally
        {
            try
            {
                if (batchId is not null)
                {
                    await importer.RollbackAsync(batchId.Value);
                }
            }
            finally
            {
                File.Delete(workbookPath);
            }
        }
    }

    private static DebtWorkbookImportPlan CreatePlan(string description, decimal secondInstallmentAmount = 20m)
    {
        var debtKey = Hash(description);
        return new DebtWorkbookImportPlan(
            [
                new DebtWorkbookDebt(
                    debtKey,
                    description,
                    new DateTime(2026, 1, 10),
                    30m,
                    2,
                    [
                        new DebtWorkbookInstallment(
                            Hash($"{debtKey}|1"),
                            1,
                            new DateTime(2026, 1, 10),
                            10m,
                            "Pendente",
                            null),
                        new DebtWorkbookInstallment(
                            Hash($"{debtKey}|2"),
                            2,
                            new DateTime(2026, 2, 10),
                            secondInstallmentAmount,
                            "Pendente",
                            null)
                    ])
            ],
            UndatedEarlyPaymentsSkipped: 0);
    }

    private static string FindMigrationScriptsDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "ControleFinanceiro.Migrations", "Scripts");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("The migration scripts directory was not found.");
    }

    private static async Task<int> CountAsync(
        string connectionString,
        string sql,
        (string Name, object Value) parameter)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
