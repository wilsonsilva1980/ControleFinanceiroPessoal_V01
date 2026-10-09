namespace ControleFinanceiro.Migrations.Migrations;

public sealed class MigrationValidationException : Exception
{
	public MigrationValidationException()
	{
	}

	public MigrationValidationException(string message)
		: base(message)
	{
	}

	public MigrationValidationException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

}

public sealed class MigrationConcurrencyException : Exception
{
	public MigrationConcurrencyException()
	{
	}

	public MigrationConcurrencyException(string message)
		: base(message)
	{
	}

	public MigrationConcurrencyException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

}
