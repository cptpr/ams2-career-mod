namespace Ams2CareerCompanion.App.ViewModels;

public sealed class GarageCarCardViewModel
{
    public string Name { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string ConditionText { get; init; } = string.Empty;
    public string EligibleEventsText { get; init; } = string.Empty;
    public string CareerStartsText { get; init; } = string.Empty;
    public string PriceText { get; init; } = string.Empty;
    public string UnlockText { get; init; } = string.Empty;
    public string ActionText { get; init; } = string.Empty;
    public string SummaryText { get; init; } = string.Empty;
    public bool CanAct { get; init; }
    public bool IsLocked { get; init; }
    public bool IsOwned { get; init; }
}
