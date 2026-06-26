using System.Windows.Input;

namespace Ams2CareerCompanion.App.ViewModels;

public sealed class DeskActionViewModel : ObservableObject
{
    private string _label = string.Empty;
    private ICommand? _command;
    private bool _isVisible;
    private bool _isEnabled;
    private string _tone = "Neutral";

    public string Label
    {
        get => _label;
        private set => SetProperty(ref _label, value);
    }

    public ICommand? Command
    {
        get => _command;
        private set => SetProperty(ref _command, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        private set => SetProperty(ref _isEnabled, value);
    }

    public string Tone
    {
        get => _tone;
        private set => SetProperty(ref _tone, value);
    }

    public void Clear()
    {
        Label = string.Empty;
        Command = null;
        IsVisible = false;
        IsEnabled = false;
        Tone = "Neutral";
    }

    public void Set(string label, ICommand? command, bool isEnabled, string tone)
    {
        Label = label;
        Command = command;
        IsVisible = true;
        IsEnabled = isEnabled;
        Tone = tone;
    }
}
