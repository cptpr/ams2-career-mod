using System.Collections.ObjectModel;
using System.Windows;
using Ams2CareerCompanion.Core.Interfaces;
using Ams2CareerCompanion.Core.Models;
using Ams2CareerCompanion.Core.Services;
using Ams2CareerCompanion.Infrastructure.Diagnostics;
using Ams2CareerCompanion.Infrastructure.Launch;

namespace Ams2CareerCompanion.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    private const int CareerNameMaxLength = 32;

    private readonly CareerContentCatalog _content;
    private readonly ICareerRepository _repository;
    private readonly IGameTelemetryFeed _telemetryFeed;
    private readonly IMockRaceSimulator _mockRaceSimulator;
    private readonly Ams2LaunchService _launchService;
    private readonly Ams2SessionPresetService _sessionPresetService;
    private readonly RaceAutomationCoordinator _raceAutomationCoordinator;
    private readonly RaceAutomationTraceWriter _automationTraceWriter;
    private readonly ChampionshipEditorPresetExportAdapter _eventExportAdapter;
    private readonly CareerFactory _careerFactory;
    private readonly CareerEventPlanner _eventPlanner;
    private readonly CareerProgressionEngine _progressionEngine;
    private readonly RelayCommand _createCareerCommand;
    private readonly RelayCommand _launchAms2Command;
    private readonly RelayCommand _launchAms2VrCommand;
    private readonly RelayCommand _applyRecommendedEventCommand;
    private readonly RelayCommand _applyRecommendedEventAndLaunchCommand;
    private readonly RelayCommand _launchWithPresetCommand;
    private readonly RelayCommand _capturePresetCommand;
    private readonly RelayCommand _applyPresetCommand;
    private readonly RelayCommand _deletePresetCommand;
    private readonly RelayCommand _loadSelectedCareerCommand;
    private readonly RelayCommand _deleteSelectedCareerCommand;
    private readonly RelayCommand _simulateRaceCommand;
    private readonly RelayCommand _simulateUncertainRaceCommand;
    private readonly RelayCommand _confirmPendingResultCommand;
    private readonly RelayCommand _refreshCommand;

    private CareerState? _career;
    private CareerSummary? _selectedCareerSummary;
    private RaceHistoryEntry? _selectedRecentResult;
    private RaceResultDraft? _pendingDraft;
    private CareerEventPlan? _nextEventPlan;
    private StarterCarDefinition? _selectedStarterCar;
    private SessionPresetInfo? _selectedSessionPreset;
    private string _sessionPresetNameInput = "rookie-cup";
    private string _careerNameInput = "New Career";
    private string _connectionStateText = "Disconnected";
    private string _sessionSummary = "AMS2 not connected yet.";
    private string _statusMessage = "Create a career or load the existing save to begin.";
    private string _rewardSummary = "No race rewards yet.";
    private string _featuredRivalText = "No rival selected yet.";
    private string _launcherStatusText = "AMS2 launcher not checked yet.";
    private string _installPathText = "Not detected yet.";
    private string _nextEventHeadlineText = "No next event available.";
    private string _nextEventDetailsText = "Create or load a career to generate the next event plan.";
    private string _nextEventExportStatusText = "No export target selected yet.";
    private string _automationHeadlineText = "No race automation active.";
    private string _automationDetailText = "Generate or load a career event to prepare the next race.";
    private string _automationTelemetryText = string.Empty;
    private bool _developerToolsEnabled;

    public MainViewModel(
        CareerContentCatalog content,
        ICareerRepository repository,
        IGameTelemetryFeed telemetryFeed,
        IMockRaceSimulator mockRaceSimulator,
        Ams2LaunchService launchService,
        Ams2SessionPresetService sessionPresetService,
        RaceAutomationCoordinator raceAutomationCoordinator,
        RaceAutomationTraceWriter automationTraceWriter,
        ResultReconstructionService resultReconstructionService,
        CareerFactory careerFactory,
        CareerProgressionEngine progressionEngine)
    {
        _content = content;
        _repository = repository;
        _telemetryFeed = telemetryFeed;
        _mockRaceSimulator = mockRaceSimulator;
        _launchService = launchService;
        _sessionPresetService = sessionPresetService;
        _raceAutomationCoordinator = raceAutomationCoordinator;
        _automationTraceWriter = automationTraceWriter;
        _eventExportAdapter = new ChampionshipEditorPresetExportAdapter(sessionPresetService);
        _careerFactory = careerFactory;
        _eventPlanner = new CareerEventPlanner();
        _progressionEngine = progressionEngine;

        StarterCars = new ObservableCollection<StarterCarDefinition>(_content.StarterCars);
        Careers = new ObservableCollection<CareerSummary>();
        UnlockedLeagues = new ObservableCollection<string>();
        Challenges = new ObservableCollection<string>();
        Rivals = new ObservableCollection<string>();
        RecentResults = new ObservableCollection<RaceHistoryEntry>();
        RaceHistory = new ObservableCollection<RaceHistoryEntry>();
        SessionPresets = new ObservableCollection<SessionPresetInfo>();

        _createCareerCommand = new RelayCommand(CreateCareerAsync, () => SelectedStarterCar is not null && !string.IsNullOrWhiteSpace(CareerNameInput));
        _launchAms2Command = new RelayCommand(LaunchAms2Async);
        _launchAms2VrCommand = new RelayCommand(LaunchAms2VrAsync);
        _applyRecommendedEventCommand = new RelayCommand(ApplyRecommendedEventAsync, () => _nextEventPlan is not null);
        _applyRecommendedEventAndLaunchCommand = new RelayCommand(ApplyRecommendedEventAndLaunchAsync, () => _nextEventPlan is not null);
        _launchWithPresetCommand = new RelayCommand(LaunchWithPresetAsync, () => SelectedSessionPreset is not null);
        _capturePresetCommand = new RelayCommand(CapturePresetAsync, () => !string.IsNullOrWhiteSpace(SessionPresetNameInput));
        _applyPresetCommand = new RelayCommand(ApplyPresetAsync, () => SelectedSessionPreset is not null);
        _deletePresetCommand = new RelayCommand(DeletePresetAsync, () => SelectedSessionPreset is not null && SelectedSessionPreset.CanDelete);
        _loadSelectedCareerCommand = new RelayCommand(LoadSelectedCareerAsync, () => SelectedCareerSummary is not null);
        _deleteSelectedCareerCommand = new RelayCommand(DeleteSelectedCareerAsync, () => SelectedCareerSummary is not null);
        _simulateRaceCommand = new RelayCommand(() => SimulateRaceAsync(false), () => _career is not null);
        _simulateUncertainRaceCommand = new RelayCommand(() => SimulateRaceAsync(true), () => _career is not null);
        _confirmPendingResultCommand = new RelayCommand(ConfirmPendingResultAsync, () => PendingDraft is not null);
        _refreshCommand = new RelayCommand(RefreshAsync);

        telemetryFeed.SessionStatusChanged += OnSessionStatusChanged;
        resultReconstructionService.DraftCreated += OnDraftCreated;
        raceAutomationCoordinator.StatusChanged += OnAutomationStatusChanged;
    }

    public ObservableCollection<StarterCarDefinition> StarterCars { get; }
    public ObservableCollection<CareerSummary> Careers { get; }
    public ObservableCollection<string> UnlockedLeagues { get; }
    public ObservableCollection<string> Challenges { get; }
    public ObservableCollection<string> Rivals { get; }
    public ObservableCollection<RaceHistoryEntry> RecentResults { get; }
    public ObservableCollection<RaceHistoryEntry> RaceHistory { get; }
    public ObservableCollection<SessionPresetInfo> SessionPresets { get; }

    public RelayCommand CreateCareerCommand => _createCareerCommand;
    public RelayCommand LaunchAms2Command => _launchAms2Command;
    public RelayCommand LaunchAms2VrCommand => _launchAms2VrCommand;
    public RelayCommand ApplyRecommendedEventCommand => _applyRecommendedEventCommand;
    public RelayCommand ApplyRecommendedEventAndLaunchCommand => _applyRecommendedEventAndLaunchCommand;
    public RelayCommand LaunchWithPresetCommand => _launchWithPresetCommand;
    public RelayCommand CapturePresetCommand => _capturePresetCommand;
    public RelayCommand ApplyPresetCommand => _applyPresetCommand;
    public RelayCommand DeletePresetCommand => _deletePresetCommand;
    public RelayCommand LoadSelectedCareerCommand => _loadSelectedCareerCommand;
    public RelayCommand DeleteSelectedCareerCommand => _deleteSelectedCareerCommand;
    public RelayCommand SimulateRaceCommand => _simulateRaceCommand;
    public RelayCommand SimulateUncertainRaceCommand => _simulateUncertainRaceCommand;
    public RelayCommand ConfirmPendingResultCommand => _confirmPendingResultCommand;
    public RelayCommand RefreshCommand => _refreshCommand;

    public string CareerNameInput
    {
        get => _careerNameInput;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Length > CareerNameMaxLength
                    ? value[..CareerNameMaxLength]
                    : value;

            if (SetProperty(ref _careerNameInput, normalized))
            {
                _createCareerCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(CareerNameCharacterCount));
            }
        }
    }

    public StarterCarDefinition? SelectedStarterCar
    {
        get => _selectedStarterCar;
        set
        {
            if (SetProperty(ref _selectedStarterCar, value))
            {
                _createCareerCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SessionPresetNameInput
    {
        get => _sessionPresetNameInput;
        set
        {
            if (SetProperty(ref _sessionPresetNameInput, value))
            {
                _capturePresetCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public SessionPresetInfo? SelectedSessionPreset
    {
        get => _selectedSessionPreset;
        set
        {
            if (SetProperty(ref _selectedSessionPreset, value))
            {
                _launchWithPresetCommand.RaiseCanExecuteChanged();
                _applyPresetCommand.RaiseCanExecuteChanged();
                _deletePresetCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(SelectedSessionPresetSummaryText));
                RaisePropertyChanged(nameof(CanDeleteSelectedSessionPreset));
            }
        }
    }

    public CareerSummary? SelectedCareerSummary
    {
        get => _selectedCareerSummary;
        set
        {
            if (SetProperty(ref _selectedCareerSummary, value))
            {
                _loadSelectedCareerCommand.RaiseCanExecuteChanged();
                _deleteSelectedCareerCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(SelectedCareerSummaryText));
            }
        }
    }

    public RaceHistoryEntry? SelectedRecentResult
    {
        get => _selectedRecentResult;
        set
        {
            if (SetProperty(ref _selectedRecentResult, value))
            {
                RaisePropertyChanged(nameof(SelectedRecentResultHeadline));
                RaisePropertyChanged(nameof(SelectedRecentResultDetails));
            }
        }
    }

    public string ConnectionStateText
    {
        get => _connectionStateText;
        private set => SetProperty(ref _connectionStateText, value);
    }

    public string SessionSummary
    {
        get => _sessionSummary;
        private set => SetProperty(ref _sessionSummary, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string RewardSummary
    {
        get => _rewardSummary;
        private set => SetProperty(ref _rewardSummary, value);
    }

    public string LauncherStatusText
    {
        get => _launcherStatusText;
        private set => SetProperty(ref _launcherStatusText, value);
    }

    public string InstallPathText
    {
        get => _installPathText;
        private set => SetProperty(ref _installPathText, value);
    }

    public string FeaturedRivalText
    {
        get => _featuredRivalText;
        private set => SetProperty(ref _featuredRivalText, value);
    }

    public string NextEventHeadlineText
    {
        get => _nextEventHeadlineText;
        private set => SetProperty(ref _nextEventHeadlineText, value);
    }

    public string NextEventDetailsText
    {
        get => _nextEventDetailsText;
        private set => SetProperty(ref _nextEventDetailsText, value);
    }

    public string NextEventExportStatusText
    {
        get => _nextEventExportStatusText;
        private set => SetProperty(ref _nextEventExportStatusText, value);
    }

    public string AutomationHeadlineText
    {
        get => _automationHeadlineText;
        private set => SetProperty(ref _automationHeadlineText, value);
    }

    public string AutomationDetailText
    {
        get => _automationDetailText;
        private set => SetProperty(ref _automationDetailText, value);
    }

    public string AutomationTelemetryText
    {
        get => _automationTelemetryText;
        private set => SetProperty(ref _automationTelemetryText, value);
    }

    public bool DeveloperToolsEnabled
    {
        get => _developerToolsEnabled;
        set
        {
            if (SetProperty(ref _developerToolsEnabled, value))
            {
                RaisePropertyChanged(nameof(DeveloperToolsVisibilityText));
            }
        }
    }

    public string DeveloperToolsVisibilityText => DeveloperToolsEnabled ? "Developer tools are visible." : "Developer tools are hidden.";
    public string AvailableCareersText => Careers.Count == 0 ? "No saved careers yet." : $"{Careers.Count} saved career profile(s).";
    public string RecentResultsHeaderText => RecentResults.Count == 0 ? "No past races logged yet." : $"{RecentResults.Count} logged race result(s).";
    public string RaceHistoryHeaderText => RaceHistory.Count == 0 ? "No season log yet." : $"{RaceHistory.Count} total logged race(s).";
    public string ActiveCareerSummaryText => _career is null
        ? "No active career loaded."
        : $"Active profile: {_career.Name}  |  Level {_career.Progression.Level}  |  {CurrentLeagueName}";
    public string SelectedCareerSummaryText => SelectedCareerSummary is null
        ? "No profile selected."
        : $"Selected profile: {SelectedCareerSummary.Name}  |  Level {SelectedCareerSummary.Level}";
    public string SeasonSummaryText => BuildSeasonSummary();
    public string SessionPresetStatusText => BuildSessionPresetStatusText();
    public string SelectedSessionPresetSummaryText => SelectedSessionPreset is null
        ? "No session preset selected."
        : $"{SelectedSessionPreset.SourceLabel}: {SelectedSessionPreset.Name}\n{SelectedSessionPreset.Description}";
    public bool CanDeleteSelectedSessionPreset => SelectedSessionPreset?.CanDelete == true;

    public string CurrentCareerName => _career?.Name ?? "No active career";
    public string CurrentStarterCar => _career?.PlayerProfile.StarterCarName ?? "Choose a starter car";
    public string CurrentTitle => GetCurrentTitle();
    public string CurrentLeagueName => _career is null ? "No active league" : _content.Leagues.First(x => x.Id == _career.ActiveLeagueId).Name;
    public string ProgressText => _career is null
        ? "Level 0"
        : $"Level {_career.Progression.Level}  |  XP {_career.Progression.Xp}  |  Credits {_career.Progression.Credits:n0}";
    public string StandingText => _career is null
        ? "Driver rating not established."
        : $"Driver Rating {_career.Progression.DriverRating:n0}  |  Reputation {_career.Progression.Reputation}";
    public string CareerNameCharacterCount => $"{CareerNameInput.Length}/{CareerNameMaxLength}";

    public RaceResultDraft? PendingDraft
    {
        get => _pendingDraft;
        private set
        {
            if (SetProperty(ref _pendingDraft, value))
            {
                RaisePropertyChanged(nameof(HasPendingDraft));
                RaisePropertyChanged(nameof(PendingDraftSummary));
                _confirmPendingResultCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasPendingDraft => PendingDraft is not null;
    public string PendingDraftSummary => PendingDraft?.Summary ?? "No pending result review.";
    public string SelectedRecentResultHeadline => SelectedRecentResult?.Headline ?? "Select a race result";
    public string SelectedRecentResultDetails => SelectedRecentResult?.Details ?? "Finish a race or select one from the list to inspect the full stored result details.";

    public async Task InitializeAsync()
    {
        SelectedStarterCar = StarterCars.FirstOrDefault();
        _career = await _repository.LoadCurrentCareerAsync();

        await _telemetryFeed.StartAsync();
        await RefreshAsync();
        RaiseCommandStates();
    }

    public async Task RefreshAsync()
    {
        RefreshLauncherState();
        ConnectionStateText = BuildConnectionStateText();
        await RefreshCareerListAsync();
        RefreshSessionPresetList();

        if (_career is null)
        {
            StatusMessage = "Create a new career from the Career tab, then launch AMS2 from the Race tab.";
            RefreshCollections(Array.Empty<RaceResultConfirmed>());
            RaiseCareerProperties();
            RaiseCommandStates();
            return;
        }

        var results = await _repository.LoadRaceHistoryAsync(_career.Id);
        RefreshCollections(results);
        RaiseCareerProperties();
        RefreshNextEventPlan(results);
        StatusMessage = $"Active league: {CurrentLeagueName}. {NextEventHeadlineText}";
        RaiseCommandStates();
    }

    private async Task CreateCareerAsync()
    {
        if (SelectedStarterCar is null)
        {
            return;
        }

        _career = _careerFactory.CreateNewCareer(CareerNameInput, SelectedStarterCar, CareerPreset.Standard, _content);
        await _repository.SaveCareerAsync(_career, setAsCurrent: true);
        RewardSummary = "Career created. Your first objective is to complete the Rookie Cup.";
        StatusMessage = "Career created successfully. Go to Race and launch AMS2.";
        await RefreshAsync();
        RaiseCommandStates();
    }

    private async Task LaunchAms2Async()
    {
        var launchResult = _launchService.Launch();
        RefreshLauncherState();
        ConnectionStateText = BuildConnectionStateText();
        StatusMessage = launchResult.Message;
        SessionSummary = launchResult.Success
            ? NextEventDetailsText
            : "AMS2 launch could not be started. Check the detected install path in Settings.";
        await Task.CompletedTask;
    }

    private async Task LaunchAms2VrAsync()
    {
        var launchResult = _launchService.LaunchVr();
        RefreshLauncherState();
        ConnectionStateText = BuildConnectionStateText();
        StatusMessage = launchResult.Message;
        SessionSummary = launchResult.Success
            ? NextEventDetailsText
            : "VR launch could not be started. Check the detected install path in Settings.";
        await Task.CompletedTask;
    }

    private async Task ApplyRecommendedEventAsync()
    {
        var result = _raceAutomationCoordinator.PrepareEvent(_nextEventPlan);
        NextEventExportStatusText = BuildExportStatusText(result.Message, result.Readiness, result.RequiresGameRestart);
        StatusMessage = result.Message;

        if (result.Success && result.Preset is not null)
        {
            SelectedSessionPreset = SessionPresets.FirstOrDefault(x => x.Key == result.Preset.Key) ?? result.Preset;
        }

        await Task.CompletedTask;
    }

    private async Task ApplyRecommendedEventAndLaunchAsync()
    {
        var (applyResult, launchResult) = _raceAutomationCoordinator.PrepareAndLaunch(_nextEventPlan, vr: false);
        NextEventExportStatusText = BuildExportStatusText(applyResult.Message, applyResult.Readiness, applyResult.RequiresGameRestart);

        if (!applyResult.Success)
        {
            StatusMessage = applyResult.Message;
            return;
        }

        if (applyResult.Preset is not null)
        {
            SelectedSessionPreset = SessionPresets.FirstOrDefault(x => x.Key == applyResult.Preset.Key) ?? applyResult.Preset;
        }

        StatusMessage = launchResult.Success
            ? $"{applyResult.Message} AMS2 launch requested."
            : $"{applyResult.Message} {launchResult.Message}";
        SessionSummary = launchResult.Success
            ? NextEventDetailsText
            : "Event export succeeded, but AMS2 launch failed.";
        RefreshLauncherState();
        await Task.CompletedTask;
    }

    private async Task LaunchWithPresetAsync()
    {
        if (SelectedSessionPreset is null)
        {
            return;
        }

        var presetResult = _sessionPresetService.ApplyPreset(SelectedSessionPreset);
        if (!presetResult.Success)
        {
            StatusMessage = presetResult.Message;
            return;
        }

        var launchResult = _launchService.Launch();
        RefreshLauncherState();
        StatusMessage = launchResult.Success
            ? $"Applied preset '{SelectedSessionPreset.Name}' and launched AMS2. This prepares the game state without menu macros."
            : $"{presetResult.Message} {launchResult.Message}";
        SessionSummary = launchResult.Success
            ? "AMS2 launched with a restored championship/custom-event editor state. Verify the prepared event in-game and start when ready."
            : "Preset was applied, but AMS2 launch failed.";
        await Task.CompletedTask;
    }

    private async Task CapturePresetAsync()
    {
        var result = _sessionPresetService.CaptureCurrentPreset(SessionPresetNameInput);
        StatusMessage = result.Message;
        RefreshSessionPresetList();

        if (result.Success && !string.IsNullOrWhiteSpace(result.PresetName))
        {
            SelectedSessionPreset = SessionPresets.FirstOrDefault(x =>
                x.Source == SessionPresetSource.User &&
                string.Equals(x.Slug, result.PresetName, StringComparison.OrdinalIgnoreCase));
        }

        await Task.CompletedTask;
    }

    private async Task ApplyPresetAsync()
    {
        var result = _sessionPresetService.ApplyPreset(SelectedSessionPreset);
        StatusMessage = result.Message;
        await Task.CompletedTask;
    }

    private async Task DeletePresetAsync()
    {
        if (SelectedSessionPreset is null)
        {
            return;
        }

        if (!SelectedSessionPreset.CanDelete)
        {
            StatusMessage = "Built-in presets cannot be deleted.";
            return;
        }

        var selected = SelectedSessionPreset;
        var confirm = MessageBox.Show(
            $"Delete session preset '{selected.Name}'?",
            "Delete Session Preset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var result = _sessionPresetService.DeletePreset(selected);
        StatusMessage = result.Message;
        RefreshSessionPresetList();
        SelectedSessionPreset = SessionPresets.FirstOrDefault();
        await Task.CompletedTask;
    }

    private async Task LoadSelectedCareerAsync()
    {
        if (SelectedCareerSummary is null)
        {
            return;
        }

        await _repository.SetCurrentCareerAsync(SelectedCareerSummary.Id);
        _career = await _repository.LoadCurrentCareerAsync();
        StatusMessage = $"Loaded career: {SelectedCareerSummary.Name}.";
        await RefreshAsync();
    }

    private async Task DeleteSelectedCareerAsync()
    {
        if (SelectedCareerSummary is null)
        {
            return;
        }

        var selected = SelectedCareerSummary;
        var confirm = MessageBox.Show(
            $"Delete career profile '{selected.Name}' and all of its logged race history?",
            "Delete Career Profile",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        await _repository.DeleteCareerAsync(selected.Id);
        _career = await _repository.LoadCurrentCareerAsync();
        StatusMessage = $"Deleted career: {selected.Name}.";
        await RefreshAsync();
    }

    private async Task SimulateRaceAsync(bool forceManualReview)
    {
        if (_career is null)
        {
            return;
        }

        var league = _content.Leagues.First(x => x.Id == _career.ActiveLeagueId);
        var plannedPosition = BuildPlannedFinishPosition(_career.Progression.DriverRating, league.GridSize);
        var plannedClassPosition = Math.Max(1, plannedPosition - 1);

        StatusMessage = forceManualReview
            ? "Developer simulation: uncertain result path."
            : "Developer simulation: clean result path.";

        await _mockRaceSimulator.RunSimulationAsync(new MockRaceScenario
        {
            LeagueId = league.Id,
            LeagueName = league.Name,
            TrackName = league.TrackName,
            GridSize = league.GridSize,
            PlannedFinishPosition = plannedPosition,
            PlannedClassPosition = plannedClassPosition,
            ForceManualReview = forceManualReview,
            IsCleanRace = Random.Shared.NextDouble() >= 0.22
        });
    }

    private async Task ConfirmPendingResultAsync()
    {
        if (PendingDraft is null)
        {
            return;
        }

        await CommitDraftAsync(PendingDraft, true);
        PendingDraft = null;
        RaiseCommandStates();
    }

    private void OnSessionStatusChanged(object? sender, SessionStatusSnapshot snapshot)
    {
        _ = Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _automationTraceWriter.AppendSessionSnapshot(_raceAutomationCoordinator.CurrentStatus.RunId, snapshot);
            var leagueName = ResolveSessionLeagueName(snapshot);
            var trackName = ResolveSessionTrackName(snapshot);

            ConnectionStateText = BuildConnectionStateText();
            SessionSummary = snapshot.SessionPhase switch
            {
                SessionPhase.Grid => $"Grid formed for {leagueName} at {trackName}.",
                SessionPhase.Running => $"Race running: {leagueName} at {trackName}.",
                SessionPhase.Finished => $"Session finished at {trackName}. Evaluating result confidence.",
                _ => snapshot.IsConnected ? "Monitoring AMS2 for the next session." : trackName
            };
        });
    }

    private async void OnDraftCreated(object? sender, RaceResultDraft draft)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (draft.Confidence == ResultConfidence.High)
            {
                await CommitDraftAsync(draft, reviewed: false);
                return;
            }

            PendingDraft = draft;
            StatusMessage = "Result confidence is below automatic commit threshold. Review and confirm the detected finish.";
            RaiseCommandStates();
        });
    }

    private void OnAutomationStatusChanged(object? sender, RaceAutomationStatus status)
    {
        _ = Application.Current.Dispatcher.InvokeAsync(() =>
        {
            AutomationHeadlineText = status.Headline;
            AutomationDetailText = status.Detail;
            AutomationTelemetryText = status.TelemetryLine;

            if (status.Stage == RaceAutomationStage.SessionFinished)
            {
                StatusMessage = status.Headline;
            }
        });
    }

    private async Task CommitDraftAsync(RaceResultDraft draft, bool reviewed)
    {
        if (_career is null)
        {
            return;
        }

        var activeLeague = _content.Leagues.FirstOrDefault(x => x.Id == _career.ActiveLeagueId);
        var normalizedDraft = new RaceResultDraft
        {
            Id = draft.Id,
            AutomationRunId = draft.AutomationRunId,
            LeagueId = string.IsNullOrWhiteSpace(draft.LeagueId) ? _career.ActiveLeagueId : draft.LeagueId,
            LeagueName = string.IsNullOrWhiteSpace(draft.LeagueName) ? activeLeague?.Name ?? draft.LeagueName : draft.LeagueName,
            TrackName = string.IsNullOrWhiteSpace(draft.TrackName) ? activeLeague?.TrackName ?? draft.TrackName : draft.TrackName,
            CompletedUtc = draft.CompletedUtc,
            Outcome = draft.Outcome,
            OverallPosition = draft.OverallPosition,
            ClassPosition = draft.ClassPosition,
            Entrants = draft.Entrants,
            LapsCompleted = draft.LapsCompleted,
            IsCleanRace = draft.IsCleanRace,
            Confidence = draft.Confidence,
            Summary = draft.Summary
        };

        var confirmed = new RaceResultConfirmed
        {
            Draft = normalizedDraft,
            WasReviewed = reviewed
        };

        if (await _repository.HasLoggedRaceAsync(_career.Id, confirmed.Draft.Id, confirmed.Draft.AutomationRunId))
        {
            StatusMessage = confirmed.Draft.AutomationRunId is null
                ? "This race result was already logged."
                : "This automation run already produced a logged race result.";
            PendingDraft = null;
            RaiseCommandStates();
            return;
        }

        var update = _progressionEngine.ApplyRaceResult(_career, confirmed, _content);
        await _repository.SaveCareerAsync(_career, setAsCurrent: true);
        await _repository.AppendRaceResultAsync(_career.Id, confirmed);
        _automationTraceWriter.AppendCommittedResult(_career.Id, confirmed);

        FeaturedRivalText = update.FeaturedRival is null
            ? "No featured rival available."
            : $"{update.FeaturedRival.Name}  |  {update.FeaturedRival.Personality}  |  Rivalry {update.FeaturedRival.RivalryIntensity}";

        RewardSummary = BuildRewardSummary(update.RewardBreakdown);
        StatusMessage = update.RewardBreakdown.NewlyActiveLeagueName is null
            ? "Race committed automatically."
            : $"Race committed. Next recommended league: {update.RewardBreakdown.NewlyActiveLeagueName}.";

        var results = await _repository.LoadRaceHistoryAsync(_career.Id);
        RefreshCollections(results);
        RaiseCareerProperties();
        RaiseCommandStates();
    }

    private void RefreshCollections(IReadOnlyList<RaceResultConfirmed> results)
    {
        UnlockedLeagues.Clear();
        Challenges.Clear();
        Rivals.Clear();
        RecentResults.Clear();
        RaceHistory.Clear();
        SelectedRecentResult = null;

        if (_career is null)
        {
            RaisePropertyChanged(nameof(RecentResultsHeaderText));
            RaisePropertyChanged(nameof(RaceHistoryHeaderText));
            RaisePropertyChanged(nameof(SeasonSummaryText));
            return;
        }

        foreach (var leagueId in _career.UnlockedLeagueIds)
        {
            var league = _content.Leagues.FirstOrDefault(x => x.Id == leagueId);
            if (league is not null)
            {
                var completed = _career.CompletedLeagueIds.Contains(leagueId, StringComparer.Ordinal) ? "Completed" : "Unlocked";
                UnlockedLeagues.Add($"{league.Name}  |  {league.ClassName}  |  {completed}");
            }
        }

        foreach (var challenge in _career.Challenges)
        {
            var state = challenge.IsCompleted ? "Done" : $"{challenge.Progress}/{challenge.Target}";
            Challenges.Add($"{challenge.Name}  |  {state}");
        }

        foreach (var rival in _career.Rivals.OrderByDescending(x => x.RivalryIntensity))
        {
            Rivals.Add($"{rival.Name}  |  {rival.Specialty}  |  Rating {rival.DriverRating:n0}  |  Rivalry {rival.RivalryIntensity}");
        }

        foreach (var result in results.Take(12))
        {
            RecentResults.Add(RaceHistoryEntry.From(result));
        }

        foreach (var result in results)
        {
            RaceHistory.Add(RaceHistoryEntry.From(result));
        }

        SelectedRecentResult = RecentResults.FirstOrDefault();

        FeaturedRivalText = _career.Rivals.Count == 0
            ? "No rival selected yet."
            : $"{_career.Rivals.OrderByDescending(x => x.RivalryIntensity).First().Name} is your current featured rival.";
        RefreshNextEventPlan(results);
        RaisePropertyChanged(nameof(RecentResultsHeaderText));
        RaisePropertyChanged(nameof(RaceHistoryHeaderText));
        RaisePropertyChanged(nameof(SeasonSummaryText));
    }

    private static string BuildRewardSummary(RewardBreakdown reward)
    {
        var unlockText = reward.Unlocks.Count == 0
            ? "No new unlocks."
            : string.Join(" | ", reward.Unlocks);

        var challengeText = reward.CompletedChallenges.Count == 0
            ? "No challenge completions."
            : string.Join(", ", reward.CompletedChallenges);

        return $"XP +{reward.XpDelta}, Credits +{reward.CreditsDelta}, Rating {reward.DriverRatingDelta:+0.0;-0.0;0}, Rep +{reward.ReputationDelta}. " +
               $"{challengeText}  |  {unlockText}";
    }

    private string GetCurrentTitle()
    {
        if (_career is null || _career.UnlockedTitleIds.Count == 0)
        {
            return "No title";
        }

        var unlocked = _content.Titles
            .Where(x => _career.UnlockedTitleIds.Contains(x.Id, StringComparer.Ordinal))
            .OrderByDescending(x => x.RequiredLevel)
            .FirstOrDefault();

        return unlocked?.Name ?? "No title";
    }

    private void RaiseCareerProperties()
    {
        RaisePropertyChanged(nameof(CurrentCareerName));
        RaisePropertyChanged(nameof(CurrentStarterCar));
        RaisePropertyChanged(nameof(CurrentTitle));
        RaisePropertyChanged(nameof(CurrentLeagueName));
        RaisePropertyChanged(nameof(ProgressText));
        RaisePropertyChanged(nameof(StandingText));
        RaisePropertyChanged(nameof(ActiveCareerSummaryText));
        RaisePropertyChanged(nameof(SeasonSummaryText));
        RaisePropertyChanged(nameof(NextEventHeadlineText));
        RaisePropertyChanged(nameof(NextEventDetailsText));
    }

    private void RaiseCommandStates()
    {
        _createCareerCommand.RaiseCanExecuteChanged();
        _launchAms2Command.RaiseCanExecuteChanged();
        _launchAms2VrCommand.RaiseCanExecuteChanged();
        _applyRecommendedEventCommand.RaiseCanExecuteChanged();
        _applyRecommendedEventAndLaunchCommand.RaiseCanExecuteChanged();
        _launchWithPresetCommand.RaiseCanExecuteChanged();
        _capturePresetCommand.RaiseCanExecuteChanged();
        _applyPresetCommand.RaiseCanExecuteChanged();
        _deletePresetCommand.RaiseCanExecuteChanged();
        _loadSelectedCareerCommand.RaiseCanExecuteChanged();
        _deleteSelectedCareerCommand.RaiseCanExecuteChanged();
        _simulateRaceCommand.RaiseCanExecuteChanged();
        _simulateUncertainRaceCommand.RaiseCanExecuteChanged();
        _confirmPendingResultCommand.RaiseCanExecuteChanged();
    }

    private async Task RefreshCareerListAsync()
    {
        var careers = await _repository.ListCareersAsync();
        Careers.Clear();
        foreach (var career in careers)
        {
            Careers.Add(career);
        }

        if (_career is not null)
        {
            SelectedCareerSummary = Careers.FirstOrDefault(x => x.Id == _career.Id);
        }

        RaisePropertyChanged(nameof(AvailableCareersText));
        RaisePropertyChanged(nameof(SelectedCareerSummaryText));
    }

    private void RefreshSessionPresetList()
    {
        var presets = _sessionPresetService.ListPresets();
        SessionPresets.Clear();
        foreach (var preset in presets)
        {
            SessionPresets.Add(preset);
        }

        if (SelectedSessionPreset is not null && SessionPresets.Any(x => x.Key == SelectedSessionPreset.Key))
        {
            SelectedSessionPreset = SessionPresets.First(x => x.Key == SelectedSessionPreset.Key);
            RaisePropertyChanged(nameof(SelectedSessionPresetSummaryText));
        }
        else
        {
            SelectedSessionPreset = SessionPresets.FirstOrDefault();
        }

        RaisePropertyChanged(nameof(SessionPresetStatusText));
    }

    private string BuildSessionPresetStatusText()
    {
        var profileText = _sessionPresetService.ProfileDirectory is null
            ? "AMS2 profile directory not detected yet."
            : $"AMS2 profile detected at {_sessionPresetService.ProfileDirectory}.";

        var builtInCount = SessionPresets.Count(x => x.Source == SessionPresetSource.Bundled);
        var userCount = SessionPresets.Count(x => x.Source == SessionPresetSource.User);
        return $"{profileText} {builtInCount} built-in preset(s), {userCount} user preset(s).";
    }

    private string BuildSeasonSummary()
    {
        if (RaceHistory.Count == 0)
        {
            return "No races logged for this career yet.";
        }

        var wins = RaceHistory.Count(x => x.OverallPosition == 1);
        var podiums = RaceHistory.Count(x => x.OverallPosition <= 3);
        var cleanRaces = RaceHistory.Count(x => x.IsCleanRace);
        var reviewed = RaceHistory.Count(x => x.WasReviewed);
        var averageFinish = RaceHistory.Average(x => x.OverallPosition);
        var bestFinish = RaceHistory.Min(x => x.OverallPosition);
        var totalLaps = RaceHistory.Sum(x => x.LapsCompleted);

        return $"Wins {wins}  |  Podiums {podiums}  |  Best finish P{bestFinish}  |  Avg finish P{averageFinish:0.0}  |  Clean races {cleanRaces}/{RaceHistory.Count}  |  Manual reviews {reviewed}  |  Total laps {totalLaps}";
    }

    private void RefreshNextEventPlan(IReadOnlyList<RaceResultConfirmed> results)
    {
        if (_career is null)
        {
            _nextEventPlan = null;
            NextEventHeadlineText = "No next event available.";
            NextEventDetailsText = "Create or load a career to generate the next event plan.";
            NextEventExportStatusText = "No export target selected yet.";
            return;
        }

        _nextEventPlan = _eventPlanner.BuildNextEvent(_career, _content, results);
        var plan = _nextEventPlan;
        NextEventHeadlineText = $"Next Event: {plan.LeagueName} at {plan.TrackDisplayName}  |  Round {plan.EventNumber}/{plan.EventCount}";
        NextEventDetailsText =
            $"{plan.PlayerCarClassName}  |  {plan.Country}  |  Grid {plan.RecommendedGridSize}  |  " +
            $"Reward {plan.BaseXpReward} XP / {plan.BaseCreditReward:n0} credits  |  " +
            $"Template: {plan.EventTemplateName}  |  Preset target: {plan.SuggestedPresetSlug}\n{plan.SetupNotes}";

        var preview = _eventExportAdapter.BuildPreview(plan);
        NextEventExportStatusText = BuildExportStatusText(preview.Message, preview.Readiness, preview.RequiresGameRestart);
    }

    private static string BuildExportStatusText(string message, EventExportReadiness? readiness, bool requiresRestart)
    {
        if (readiness is null)
        {
            return message;
        }

        var lines = new List<string> { message };

        if (!string.IsNullOrWhiteSpace(readiness.TargetFilePath))
        {
            lines.Add($"Target file: {readiness.TargetFilePath}");
        }

        if (!string.IsNullOrWhiteSpace(readiness.PresetFilePath))
        {
            lines.Add($"Preset source: {readiness.PresetFilePath}");
        }

        if (readiness.BlockingIssues.Count > 0)
        {
            lines.Add($"Blocking: {string.Join(" | ", readiness.BlockingIssues)}");
        }

        if (readiness.Guidance.Count > 0)
        {
            lines.Add($"Guidance: {string.Join(" | ", readiness.Guidance)}");
        }

        lines.Add(requiresRestart
            ? "Launch rule: restart AMS2 after applying this prepared event."
            : "Launch rule: no restart required after applying.");

        return string.Join("\n", lines);
    }

    private string ResolveSessionLeagueName(SessionStatusSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.LeagueName))
        {
            return snapshot.LeagueName;
        }

        if (_career is not null)
        {
            var activeLeague = _content.Leagues.FirstOrDefault(x => x.Id == _career.ActiveLeagueId);
            if (activeLeague is not null)
            {
                return activeLeague.Name;
            }
        }

        return "current race";
    }

    private string ResolveSessionTrackName(SessionStatusSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.TrackName))
        {
            return snapshot.TrackName;
        }

        if (_career is not null)
        {
            var activeLeague = _content.Leagues.FirstOrDefault(x => x.Id == _career.ActiveLeagueId);
            if (activeLeague is not null && !string.IsNullOrWhiteSpace(activeLeague.TrackName))
            {
                return activeLeague.TrackName;
            }
        }

        return snapshot.IsConnected ? "the current track" : "AMS2 feed disconnected.";
    }

    private void RefreshLauncherState()
    {
        _launchService.Refresh();
        InstallPathText = _launchService.InstallDirectory ?? "AMS2 install not detected.";
        LauncherStatusText = _launchService.IsInstalled
            ? _launchService.IsGameRunning
                ? "AMS2 detected and currently running."
                : "AMS2 detected and ready to launch."
            : "AMS2 install not detected. Steam fallback will be used if available.";
    }

    private string BuildConnectionStateText()
    {
        return _telemetryFeed.ConnectionState switch
        {
            TelemetryConnectionState.Simulating => "Developer Simulation",
            TelemetryConnectionState.Monitoring => "AMS2 Connected",
            _ => _launchService.IsGameRunning ? "AMS2 Running / Feed Waiting" : "AMS2 Disconnected"
        };
    }

    private static int BuildPlannedFinishPosition(double driverRating, int gridSize)
    {
        var strengthBias = driverRating switch
        {
            >= 1080 => 0.15,
            >= 1030 => 0.30,
            >= 980 => 0.45,
            _ => 0.60
        };

        var expected = Math.Max(1, (int)Math.Round(gridSize * strengthBias));
        var variance = Random.Shared.Next(-2, 3);
        return Math.Clamp(expected + variance, 1, gridSize);
    }

    public async ValueTask DisposeAsync()
    {
        _raceAutomationCoordinator.Dispose();
        await _telemetryFeed.DisposeAsync();
    }

    public sealed class RaceHistoryEntry
    {
        public required Guid Id { get; init; }
        public required int OverallPosition { get; init; }
        public required int ClassPosition { get; init; }
        public required int Entrants { get; init; }
        public required int LapsCompleted { get; init; }
        public required bool IsCleanRace { get; init; }
        public required bool WasReviewed { get; init; }
        public Guid? AutomationRunId { get; init; }
        public required string Headline { get; init; }
        public required string Subtitle { get; init; }
        public required string ResultTag { get; init; }
        public required string CleanlinessTag { get; init; }
        public required string ReviewTag { get; init; }
        public required string DetailLine { get; init; }
        public required string Details { get; init; }

        public static RaceHistoryEntry From(RaceResultConfirmed result)
        {
            var draft = result.Draft;
            var completedLocal = draft.CompletedUtc == default
                ? "Unknown date"
                : draft.CompletedUtc.ToLocalTime().ToString("dd MMM yyyy  HH:mm");

            var resultTag = draft.OverallPosition switch
            {
                _ when draft.Outcome == RaceOutcome.Disqualified => "Disqualified",
                _ when draft.Outcome == RaceOutcome.Restarted => "Restarted",
                _ when draft.Outcome == RaceOutcome.Abandoned => "Abandoned",
                _ when draft.Outcome == RaceOutcome.Retired => "Retired",
                1 => "Win",
                2 or 3 => "Podium",
                <= 5 => "Top 5",
                <= 10 => "Top 10",
                _ => "Finish"
            };

            var cleanTag = draft.IsCleanRace ? "Clean race" : "Incident flagged";
            var reviewTag = result.WasReviewed ? "Manual review" : "Auto logged";
            var confidenceTag = $"{draft.Confidence} confidence";

            return new RaceHistoryEntry
            {
                Id = result.Id,
                OverallPosition = draft.OverallPosition,
                ClassPosition = draft.ClassPosition,
                Entrants = draft.Entrants,
                LapsCompleted = draft.LapsCompleted,
                IsCleanRace = draft.IsCleanRace,
                WasReviewed = result.WasReviewed,
                AutomationRunId = draft.AutomationRunId,
                Headline = $"{draft.LeagueName} at {draft.TrackName}",
                Subtitle = completedLocal,
                ResultTag = resultTag,
                CleanlinessTag = cleanTag,
                ReviewTag = reviewTag,
                DetailLine = $"Outcome {draft.Outcome}  |  Overall P{draft.OverallPosition}/{draft.Entrants}  |  Class P{draft.ClassPosition}  |  {draft.LapsCompleted} laps",
                Details =
                    $"League: {draft.LeagueName}\n" +
                    $"Track: {draft.TrackName}\n" +
                    $"Completed: {completedLocal}\n" +
                    $"Outcome: {draft.Outcome}\n" +
                    $"Overall finish: P{draft.OverallPosition}/{draft.Entrants}\n" +
                    $"Class finish: P{draft.ClassPosition}\n" +
                    $"Laps completed: {draft.LapsCompleted}\n" +
                    $"Race cleanliness: {cleanTag}\n" +
                    $"Capture mode: {reviewTag}\n" +
                    $"Detection quality: {confidenceTag}\n" +
                    $"Automation run: {(draft.AutomationRunId?.ToString("D") ?? "Not linked")}\n\n" +
                    $"Stored summary:\n{draft.Summary}"
            };
        }
    }
}
