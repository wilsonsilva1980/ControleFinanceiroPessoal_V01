namespace ControleFinanceiro.API.Services;

public sealed class OwnerProvisioningOptions
{
    public const string SectionName = "OwnerProvisioning";

    public string? UserName { get; set; }

    public string? Password { get; set; }
}