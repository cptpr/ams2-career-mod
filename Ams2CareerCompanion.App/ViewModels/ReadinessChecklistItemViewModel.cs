namespace Ams2CareerCompanion.App.ViewModels;

public sealed class ReadinessChecklistItemViewModel : ObservableObject
{
    private string _title = string.Empty;
    private string _detail = string.Empty;
    private string _statusText = string.Empty;
    private string _severity = "Neutral";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Detail
    {
        get => _detail;
        set => SetProperty(ref _detail, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string Severity
    {
        get => _severity;
        set => SetProperty(ref _severity, value);
    }
}
