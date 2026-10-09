using ControleFinanceiro.Migrations.Migrations;
using static Xunit.Assert;
using Xunit;

namespace ControleFinanceiro.Tests;

public class MigrationScriptDiscoveryTests
{
    [Fact]
    public void Discover_AcceptsMigrationScriptsCopiedToApplicationOutput()
    {
        var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "Scripts");

        var scripts = MigrationScriptDiscovery.Discover(scriptsDirectory);

        Assert.Contains(scripts, script => script.Version == "0002");
        Assert.Contains(scripts, script => script.Version == "0009");
    }

    [Fact]
    public void Discover_OrdersScriptsAndCalculatesChecksums()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "0002_add_index.sql"), "CREATE INDEX example;");
            File.WriteAllText(Path.Combine(directory, "0001_initial_schema.sql"), "CREATE TABLE example (id int);");

            var scripts = MigrationScriptDiscovery.Discover(directory);

            Assert.Equal(["0001", "0002"], scripts.Select(script => script.Version));
            Assert.All(scripts, script => Assert.Equal(64, script.Checksum.Length));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RecurringCadenceMigration_UsesDynamicSqlForNewRenewalDateColumn()
    {
        var scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "Scripts",
            "0007_recurring_expense_cadence.sql");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains(
            "EXEC sys.sp_executesql N'UPDATE dbo.recurring_expenses",
            script,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "EXEC sys.sp_executesql N'ALTER TABLE dbo.recurring_expenses",
            script,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Discover_RejectsDuplicateVersions()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "0001_first.sql"), "SELECT 1;");
            File.WriteAllText(Path.Combine(directory, "0001_second.sql"), "SELECT 2;");

            var exception = Assert.Throws<MigrationValidationException>(
                () => MigrationScriptDiscovery.Discover(directory));

            Assert.Contains("duplicated", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Discover_RejectsInvalidFileNames()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "initial.sql"), "SELECT 1;");

            Assert.Throws<MigrationValidationException>(
                () => MigrationScriptDiscovery.Discover(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        return Directory.CreateTempSubdirectory("controle-financeiro-migrations-").FullName;
    }
}
