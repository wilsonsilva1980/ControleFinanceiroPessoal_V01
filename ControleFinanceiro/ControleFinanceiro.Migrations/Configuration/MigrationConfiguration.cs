using Microsoft.Extensions.Configuration;

namespace ControleFinanceiro.Migrations.Configuration;

public static class MigrationConfiguration
{
    public static MigrationOptions Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new MigrationConfigurationException(
                "ConnectionStrings:DefaultConnection is required for migrations.");
        }

        var configuredScriptsDirectory = configuration["Migration:ScriptsDirectory"];
        var scriptsDirectory = string.IsNullOrWhiteSpace(configuredScriptsDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "Scripts")
            : Path.GetFullPath(configuredScriptsDirectory);

        if (!Directory.Exists(scriptsDirectory))
        {
            throw new MigrationConfigurationException(
                $"Migration scripts directory was not found: {scriptsDirectory}");
        }

        var lockTimeoutValue = configuration["Migration:LockTimeoutSeconds"];
        var lockTimeout = string.IsNullOrWhiteSpace(lockTimeoutValue)
            ? 300
            : int.TryParse(lockTimeoutValue, out var parsedLockTimeout)
                ? parsedLockTimeout
                : -1;
        if (lockTimeout is < 0 or > 3600)
        {
            throw new MigrationConfigurationException(
                "Migration:LockTimeoutSeconds must be between 0 and 3600.");
        }

        return new MigrationOptions(connectionString, scriptsDirectory, lockTimeout);
    }
}
