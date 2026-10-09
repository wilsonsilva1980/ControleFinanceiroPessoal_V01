using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ControleFinanceiro.Migrations.Migrations;

public static partial class MigrationScriptDiscovery
{
    public static IReadOnlyList<MigrationScript> Discover(string scriptsDirectory)
    {
        if (!Directory.Exists(scriptsDirectory))
        {
            throw new MigrationValidationException($"Migration scripts directory was not found: {scriptsDirectory}");
        }

        var scripts = Directory
            .EnumerateFiles(scriptsDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .Select(CreateScript)
            .OrderBy(script => script.Version, StringComparer.Ordinal)
            .ThenBy(script => script.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var duplicateVersion = scripts
            .GroupBy(script => script.Version, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateVersion is not null)
        {
            var files = string.Join(", ", duplicateVersion.Select(script => Path.GetFileName(script.FilePath)));
            throw new MigrationValidationException(
                $"Migration version '{duplicateVersion.Key}' is duplicated by: {files}.");
        }

        return scripts;
    }

    private static MigrationScript CreateScript(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var match = FileNamePattern().Match(fileName);
        if (!match.Success)
        {
            throw new MigrationValidationException(
                $"Migration script '{fileName}' must use the '<version>_<name>.sql' naming convention.");
        }

        var content = File.ReadAllText(filePath, Encoding.UTF8);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new MigrationValidationException($"Migration script '{fileName}' cannot be empty.");
        }

        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        return new MigrationScript(
            match.Groups["version"].Value,
            match.Groups["name"].Value,
            filePath,
            content,
            checksum);
    }

    [GeneratedRegex("^(?<version>[0-9]{4,})_(?<name>[A-Za-z0-9][A-Za-z0-9_-]*)\\.sql$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FileNamePattern();
}
