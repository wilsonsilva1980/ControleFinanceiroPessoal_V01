namespace ControleFinanceiro.Migrations.Migrations;

public sealed record MigrationRunResult(
    IReadOnlyList<string> Applied,
    IReadOnlyList<string> Skipped)
{
    public bool IsUpToDate => Applied.Count == 0;
}
