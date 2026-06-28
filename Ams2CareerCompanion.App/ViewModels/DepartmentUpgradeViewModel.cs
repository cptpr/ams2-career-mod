namespace Ams2CareerCompanion.App.ViewModels;

public sealed class DepartmentUpgradeViewModel
{
    public string DepartmentName { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = string.Empty;
    public string CurrentLevelText { get; init; } = string.Empty;
    public string CurrentBonusText { get; init; } = string.Empty;
    public string NextBonusText { get; init; } = string.Empty;
    public string UpgradeCostText { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string RequirementText { get; init; } = string.Empty;
    public string SummaryText { get; init; } = string.Empty;
    public bool CanUpgrade { get; init; }
    public bool IsLocked { get; init; }
}
