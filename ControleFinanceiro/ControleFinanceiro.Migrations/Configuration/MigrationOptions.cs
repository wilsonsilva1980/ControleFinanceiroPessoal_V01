namespace ControleFinanceiro.Migrations.Configuration;

public sealed record MigrationOptions(
    string ConnectionString,
    string ScriptsDirectory,
    int LockTimeoutSeconds);
