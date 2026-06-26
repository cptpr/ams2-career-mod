using System.Collections.ObjectModel;
using Ams2CareerCompanion.Infrastructure.Launch;

namespace Ams2CareerCompanion.App.ViewModels;

public sealed class RaceDeskViewModel : ObservableObject
{
    private string _connectionStateText = "AMS2 Disconnected";
    private string _sessionSummary = "AMS2 not connected yet.";
    private string _statusMessage = "Create a career or load the existing save to begin.";
    private string _rewardSummary = "No race rewards yet.";
    private string _launcherStatusText = "AMS2 launcher not checked yet.";
    private string _installPathText = "Not detected yet.";
    private string _sessionPresetStatusText = "No preset status yet.";
    private string _selectedSessionPresetSummaryText = "No session preset selected.";
    private bool _canDeleteSelectedSessionPreset;

    public RaceDeskViewModel(
        ObservableCollection<SessionPresetInfo> sessionPresets,
        ObservableCollection<RaceHistoryEntry> recentResults,
        ObservableCollection<RaceHistoryEntry> raceHistory,
        RelayCommand refreshCommand,
        RelayCommand launchAms2Command,
        RelayCommand launchAms2VrCommand,
        RelayCommand prepareRecommendedEventCommand,
        RelayCommand prepareAndLaunchRecommendedEventCommand,
        RelayCommand launchWithPresetCommand,
        RelayCommand capturePresetCommand,
        RelayCommand applyPresetCommand,
        RelayCommand deletePresetCommand,
        RelayCommand confirmPendingResultCommand,
        RelayCommand dismissPendingResultCommand)
    {
        SessionPresets = sessionPresets;
        RecentResults = recentResults;
        RaceHistory = raceHistory;
        RefreshCommand = refreshCommand;
        LaunchAms2Command = launchAms2Command;
        LaunchAms2VrCommand = launchAms2VrCommand;
        PrepareRecommendedEventCommand = prepareRecommendedEventCommand;
        PrepareAndLaunchRecommendedEventCommand = prepareAndLaunchRecommendedEventCommand;
        LaunchWithPresetCommand = launchWithPresetCommand;
        CapturePresetCommand = capturePresetCommand;
        ApplyPresetCommand = applyPresetCommand;
        DeletePresetCommand = deletePresetCommand;
        ConfirmPendingResultCommand = confirmPendingResultCommand;
        DismissPendingResultCommand = dismissPendingResultCommand;
    }

    public ObservableCollection<SessionPresetInfo> SessionPresets { get; }
    public ObservableCollection<RaceHistoryEntry> RecentResults { get; }
    public ObservableCollection<RaceHistoryEntry> RaceHistory { get; }
    public AutomationStateViewModel Automation { get; } = new();
    public EventPlanViewModel EventPlan { get; } = new();
    public ResultReviewViewModel ResultReview { get; } = new();

    public RelayCommand RefreshCommand { get; }
    public RelayCommand LaunchAms2Command { get; }
    public RelayCommand LaunchAms2VrCommand { get; }
    public RelayCommand PrepareRecommendedEventCommand { get; }
    public RelayCommand PrepareAndLaunchRecommendedEventCommand { get; }
    public RelayCommand LaunchWithPresetCommand { get; }
    public RelayCommand CapturePresetCommand { get; }
    public RelayCommand ApplyPresetCommand { get; }
    public RelayCommand DeletePresetCommand { get; }
    public RelayCommand ConfirmPendingResultCommand { get; }
    public RelayCommand DismissPendingResultCommand { get; }

    public string ConnectionStateText
    {
        get => _connectionStateText;
        set => SetProperty(ref _connectionStateText, value);
    }

    public string SessionSummary
    {
        get => _sessionSummary;
        set => SetProperty(ref _sessionSummary, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string RewardSummary
    {
        get => _rewardSummary;
        set => SetProperty(ref _rewardSummary, value);
    }

    public string LauncherStatusText
    {
        get => _launcherStatusText;
        set => SetProperty(ref _launcherStatusText, value);
    }

    public string InstallPathText
    {
        get => _installPathText;
        set => SetProperty(ref _installPathText, value);
    }

    public string SessionPresetStatusText
    {
        get => _sessionPresetStatusText;
        set => SetProperty(ref _sessionPresetStatusText, value);
    }

    public string SelectedSessionPresetSummaryText
    {
        get => _selectedSessionPresetSummaryText;
        set => SetProperty(ref _selectedSessionPresetSummaryText, value);
    }

    public bool CanDeleteSelectedSessionPreset
    {
        get => _canDeleteSelectedSessionPreset;
        set => SetProperty(ref _canDeleteSelectedSessionPreset, value);
    }
}
