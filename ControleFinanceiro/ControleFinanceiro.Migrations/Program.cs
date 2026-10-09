using ControleFinanceiro.Migrations.Configuration;
using ControleFinanceiro.DebtImport;
using ControleFinanceiro.Migrations.Migrations;

try
{
	return await RunAsync(args);
}
catch (MigrationConfigurationException exception)
{
	Console.Error.WriteLine(exception.Message);
	return 2;
}
catch (InvalidDataException exception)
{
	Console.Error.WriteLine(exception.Message);
	return 3;
}
catch (FileNotFoundException exception)
{
	Console.Error.WriteLine(exception.Message);
	return 3;
}
catch (MigrationValidationException exception)
{
	Console.Error.WriteLine(exception.Message);
	return 3;
}
catch (MigrationConcurrencyException exception)
{
	Console.Error.WriteLine(exception.Message);
	return 4;
}
catch (Exception exception)
{
	Console.Error.WriteLine($"Migration failed: {exception.Message}");
	return 1;
}

static async Task<int> RunAsync(string[] arguments)
{
	if (arguments.Length == 0)
	{
		var options = MigrationConfiguration.Load();
		var result = await new MigrationRunner(options).RunAsync();
		Console.WriteLine(result.IsUpToDate
			? "Database is up to date."
			: $"Applied migrations: {string.Join(", ", result.Applied)}.");
		return 0;
	}

	if (arguments[0].Equals("import-debts", StringComparison.OrdinalIgnoreCase))
	{
		if (arguments.Length is < 2 or > 3
			|| (arguments.Length == 3 && !arguments[2].Equals("--dry-run", StringComparison.OrdinalIgnoreCase)))
		{
			PrintUsage();
			return 2;
		}

		var workbookPath = Path.GetFullPath(arguments[1]);
		if (!Path.GetExtension(workbookPath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("Only .xlsx workbooks are supported.");
		}

		var plan = new DebtWorkbookParser().Parse(workbookPath);
		var dryRun = arguments.Length == 3;
		if (dryRun)
		{
			Console.WriteLine(
				$"Dry run: {plan.Debts.Count} debts, {plan.Debts.Sum(debt => debt.Installments.Count)} installments, "
				+ $"{plan.UndatedEarlyPaymentsSkipped} undated early payments skipped.");
			return 0;
		}

		var options = MigrationConfiguration.Load();
		var imported = await new DebtWorkbookImporter(options.ConnectionString)
			.ImportAsync(workbookPath, plan);
		Console.WriteLine(
			$"Imported {imported.DebtsInserted} debts and {imported.InstallmentsInserted} installments; "
			+ $"{imported.AlreadyImported} source records were already imported; "
			+ $"{imported.UndatedEarlyPaymentsSkipped} undated early payments were skipped.");
		if (imported.BatchId is not null)
		{
			Console.WriteLine($"Import batch: {imported.BatchId}");
		}

		return 0;
	}

	if (arguments[0].Equals("rollback-debt-import", StringComparison.OrdinalIgnoreCase))
	{
		if (arguments.Length != 2 || !Guid.TryParse(arguments[1], out var batchId))
		{
			PrintUsage();
			return 2;
		}

		var options = MigrationConfiguration.Load();
		await new DebtWorkbookImporter(options.ConnectionString).RollbackAsync(batchId);
		Console.WriteLine($"Rolled back import batch {batchId}.");
		return 0;
	}

	PrintUsage();
	return 2;
}

static void PrintUsage()
{
	Console.Error.WriteLine("Usage:");
	Console.Error.WriteLine("  ControleFinanceiro.Migrations");
	Console.Error.WriteLine("  ControleFinanceiro.Migrations import-debts <workbook.xlsx> [--dry-run]");
	Console.Error.WriteLine("  ControleFinanceiro.Migrations rollback-debt-import <batch-guid>");
}
