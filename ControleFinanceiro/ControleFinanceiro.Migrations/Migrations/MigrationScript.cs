namespace ControleFinanceiro.Migrations.Migrations;

public sealed record MigrationScript(
    string Version,
    string Name,
    string FilePath,
    string Content,
    string Checksum);
