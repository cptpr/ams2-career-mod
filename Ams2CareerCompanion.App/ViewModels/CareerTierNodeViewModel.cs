namespace Ams2CareerCompanion.App.ViewModels;

public sealed class CareerTierNodeViewModel
{
    public string TierName { get; init; } = string.Empty;
    public string SeriesName { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
    public bool IsCompleted { get; init; }
    public bool IsLocked { get; init; }
}
