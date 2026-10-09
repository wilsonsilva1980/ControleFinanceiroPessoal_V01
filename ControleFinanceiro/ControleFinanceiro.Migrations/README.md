# ControleFinanceiro.Migrations

This project applies ordered SQL Server schema migrations explicitly. The API continues to use EF Core and `AppDbContext` for runtime data access; the API does not run migrations during startup.

## Configuration

Provide the connection string outside source control through the normal .NET configuration hierarchy:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=<server>;Database=<database>;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;"
$env:Migration__LockTimeoutSeconds = "300"
```

`Migration__ScriptsDirectory` can override the default `Scripts` directory copied beside the executable. Credentials and full connection strings must not be committed or printed to logs.

## Run migrations

From the repository root:

```powershell
dotnet run --project .\Projetos\ControleFinanceiroPessoal_V01\ControleFinanceiro\ControleFinanceiro.Migrations\ControleFinanceiro.Migrations.csproj
```

The command applies pending scripts and exits successfully when the database is up to date. Configuration failures return code `2`, validation failures return code `3`, lock failures return code `4`, and SQL execution failures return code `1`.

In VS Code, run **Migrations: Run pending scripts** to execute the same project.

## Scripts

Scripts are stored in the project `Scripts` directory and use the naming convention `<version>_<name>.sql`, for example `0002_add_index.sql`. Versioned migration files are copied to the runner output and applied; unversioned SQL files are not migrations. Versions are applied in ascending order. A script that has been applied must never be edited; create a new version instead. The runner stores a SHA-256 checksum in `dbo.__MigrationHistory` and rejects changed applied scripts.

Use **Migrations: Create SQL script** in VS Code to create an empty, sequentially numbered script in this directory. Add its SQL statements before running migrations. The general workspace `Scripts` folder is not included by this migration project.

Each script runs in its own transaction together with its history record. A SQL Server application lock serializes concurrent migration processes. Failed scripts are rolled back and can be retried after the cause is fixed.

Test runs use a separate SQL Server database through `CONTROLE_FINANCEIRO_MIGRATIONS_TEST_CONNECTION`; they must never use production credentials or workbook data.

## Import historical debt installments

Apply the schema scripts first, then run a dry run before importing the workbook. From the workspace root:

```powershell
dotnet run --project .\Projetos\ControleFinanceiroPessoal_V01\ControleFinanceiro\ControleFinanceiro.Migrations\ControleFinanceiro.Migrations.csproj
dotnet run --project .\Projetos\ControleFinanceiroPessoal_V01\ControleFinanceiro\ControleFinanceiro.Migrations\ControleFinanceiro.Migrations.csproj -- import-debts .\Guias\NovaPlanilhaDividas.xlsx --dry-run
dotnet run --project .\Projetos\ControleFinanceiroPessoal_V01\ControleFinanceiro\ControleFinanceiro.Migrations\ControleFinanceiro.Migrations.csproj -- import-debts .\Guias\NovaPlanilhaDividas.xlsx
```

Workbook paths are resolved from the current working directory.
The importer reads only `Controle Pagamentos - 2024`, `Controle Pagamentos - 2025`, and `Controle Pagamentos - 2026` for installment records. It does not load the yearly overview sheets, the backup schedule, or the regular installments a second time from the anticipation sheet. It derives each debt's original value from its imported installment amounts, takes the earliest installment date as the debt due date, leaves the credit limit null, normalizes `P`/`Pago` to `Pago`, and leaves payment dates null because the schedules identify a payer but not a payment date. A positive monthly amount is included even if its installment number is blank or zero; the source-tracking key marks its number as unknown, while the target schema stores no installment number. Amounts are rounded to the target schema's two decimal places using midpoint-away-from-zero; a due day beyond the end of a month is clamped to that month's last day.

Undated completed payoff entries (`Quitação Pago` greater than zero) in `CP - 2026 - Antecipação de Parc` are not inserted into `debt_early_payments` because that table requires the actual payment date. Planned payoff values that were not paid are not counted as payments. The dry run and import summary report how many completed but undated entries were skipped. The importer prints counts and batch IDs only; it does not print workbook descriptions or amounts.

The `0009_debt_workbook_import_tracking.sql` script adds source tracking for idempotent imports and precise rollback. Import runs are transactional. If a tracked workbook record changes, roll back its batch before importing the revised workbook. To reverse a batch, use its reported ID:

```powershell
dotnet run --project .\Projetos\ControleFinanceiroPessoal_V01\ControleFinanceiro\ControleFinanceiro.Migrations\ControleFinanceiro.Migrations.csproj -- rollback-debt-import <batch-guid>
```

Rollback removes only rows created by that batch; rows that exactly matched existing database records are tracked but not deleted. Rollback is rejected if an imported debt has acquired other installments or payments.

## Web workbook upload

Apply `0009_debt_workbook_import_tracking.sql` with the migration runner before using the authenticated web import. Preview validates the selected workbook without writing imported records; the confirmed import revalidates it and uses the same transactional importer as the CLI. The API does not create or modify import-tracking schema. The import response includes the batch ID when a batch is created; use the CLI rollback command above to reverse that batch when its safety checks pass.
