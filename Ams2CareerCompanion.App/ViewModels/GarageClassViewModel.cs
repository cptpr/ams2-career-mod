namespace Ams2CareerCompanion.App.ViewModels;

public sealed class GarageClassViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Family { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsDlc { get; init; }
    public bool IsStarterEligible { get; init; }
}
