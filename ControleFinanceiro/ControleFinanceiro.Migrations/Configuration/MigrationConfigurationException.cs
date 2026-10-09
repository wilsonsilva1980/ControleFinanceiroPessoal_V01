namespace ControleFinanceiro.Migrations.Configuration;

public sealed class MigrationConfigurationException : Exception
{
	public MigrationConfigurationException()
	{
	}

	public MigrationConfigurationException(string message)
		: base(message)
	{
	}

	public MigrationConfigurationException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

}
