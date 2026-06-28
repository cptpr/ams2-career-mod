using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
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
    private readonly IEventExportAdapter _eventExportAdapter;
    private readonly CareerFactory _careerFactory;
    private readonly CareerEventPlanner _eventPlanner;
    private readonly CareerProgressionEngine _progressionEngine;
    private readonly DriverPortraitService _driverPortraitService;
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
    private readonly RelayCommand _dismissPendingResultCommand;
    private readonly RelayCommand _refreshCommand;
    private readonly RelayCommand _showRaceDeskPageCommand;
    private readonly RelayCommand _showCareerPageCommand;
    private readonly RelayCommand _showGaragePageCommand;
    private readonly RelayCommand _showTeamHqPageCommand;
    private readonly RelayCommand _showRivalsPageCommand;
    private readonly RelayCommand _showHistoryPageCommand;
    private readonly RelayCommand _showSettingsPageCommand;

    private CareerState? _career;
    private CareerSummary? _selectedCareerSummary;
    private RaceHistoryEntry? _selectedRecentResult;
    private RaceResultDraft? _pendingDraft;
    private CareerEventPlan? _nextEventPlan;
    private StarterCarDefinition? _selectedStarterCar;
    private DriverPortraitOptionViewModel? _selectedPlayerPortrait;
    private GarageCarCardViewModel? _selectedGarageCar;
    private CareerTierNodeViewModel? _selectedCareerTierNode;
    private RivalSummaryViewModel? _selectedRival;
    private DepartmentUpgradeViewModel? _selectedDepartment;
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
    private int _selectedPageIndex;

    public MainViewModel(
        CareerContentCatalog content,
        ICareerRepository repository,
        IGameTelemetryFeed telemetryFeed,
        IMockRaceSimulator mockRaceSimulator,
        Ams2LaunchService launchService,
        Ams2SessionPresetService sessionPresetService,
        RaceAutomationCoordinator raceAutomationCoordinator,
        RaceAutomationTraceWriter automationTraceWriter,
        string? telemetryLogPath,
        string startupErrorLogPath,
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
        _driverPortraitService = new DriverPortraitService();

        StarterCars = new ObservableCollection<StarterCarDefinition>(_content.StarterCars);
        PlayerPortraitOptions = new ObservableCollection<DriverPortraitOptionViewModel>(
            _driverPortraitService
                .GetPlayerPortraitOptions(_content)
                .Select(portrait => new DriverPortraitOptionViewModel
                {
                    Id = portrait.Id,
                    DisplayLabel = portrait.DisplayLabel,
                    AssetPath = ResolvePortraitAssetPath(portrait.Id),
                    Tagline = portrait.Tags.Count == 0 ? "Shared portrait pool" : string.Join("  |  ", portrait.Tags)
                }));
        Careers = new ObservableCollection<CareerSummary>();
        CareerLadderNodes = new ObservableCollection<CareerTierNodeViewModel>();
        GarageCars = new ObservableCollection<GarageCarCardViewModel>();
        OwnedCars = new ObservableCollection<GarageCarCardViewModel>();
        BuyableCars = new ObservableCollection<GarageCarCardViewModel>();
        RentalCars = new ObservableCollection<GarageCarCardViewModel>();
        LockedCars = new ObservableCollection<GarageCarCardViewModel>();
        DlcCars = new ObservableCollection<GarageCarCardViewModel>();
        GarageClasses = new ObservableCollection<GarageClassViewModel>();
        TeamHqDepartments = new ObservableCollection<DepartmentUpgradeViewModel>();
        UnlockedLeagues = new ObservableCollection<string>();
        Challenges = new ObservableCollection<string>();
        Rivals = new ObservableCollection<RivalSummaryViewModel>();
        RecentResults = new ObservableCollection<RaceHistoryEntry>();
        RaceHistory = new ObservableCollection<RaceHistoryEntry>();
        SessionPresets = new ObservableCollection<SessionPresetInfo>();

        _createCareerCommand = new RelayCommand(CreateCareerAsync, () => SelectedStarterCar is not null && SelectedPlayerPortrait is not null && !string.IsNullOrWhiteSpace(CareerNameInput));
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
        _dismissPendingResultCommand = new RelayCommand(DismissPendingResultAsync, () => PendingDraft is not null);
        _refreshCommand = new RelayCommand(RefreshAsync);
        _showRaceDeskPageCommand = new RelayCommand(() => SetSelectedPageAsync(0));
        _showCareerPageCommand = new RelayCommand(() => SetSelectedPageAsync(1));
        _showGaragePageCommand = new RelayCommand(() => SetSelectedPageAsync(2));
        _showTeamHqPageCommand = new RelayCommand(() => SetSelectedPageAsync(3));
        _showRivalsPageCommand = new RelayCommand(() => SetSelectedPageAsync(4));
        _showHistoryPageCommand = new RelayCommand(() => SetSelectedPageAsync(5));
        _showSettingsPageCommand = new RelayCommand(() => SetSelectedPageAsync(6));

        RaceDesk = new RaceDeskViewModel(
            SessionPresets,
            RecentResults,
            RaceHistory,
            _refreshCommand,
            _launchAms2Command,
            _launchAms2VrCommand,
            _applyRecommendedEventCommand,
            _applyRecommendedEventAndLaunchCommand,
            _launchWithPresetCommand,
            _capturePresetCommand,
            _applyPresetCommand,
            _deletePresetCommand,
            _confirmPendingResultCommand,
            _dismissPendingResultCommand);
        Diagnostics = new DiagnosticsSummaryViewModel();
        Diagnostics.InitializePaths(
            automationTraceWriter.TraceFilePath,
            telemetryLogPath ?? Path.Combine(AppContext.BaseDirectory, "telemetry.log"),
            startupErrorLogPath);

        telemetryFeed.SessionStatusChanged += OnSessionStatusChanged;
        resultReconstructionService.DraftCreated += OnDraftCreated;
        raceAutomationCoordinator.StatusChanged += OnAutomationStatusChanged;
    }

    public ObservableCollection<StarterCarDefinition> StarterCars { get; }
    public ObservableCollection<DriverPortraitOptionViewModel> PlayerPortraitOptions { get; }
    public ObservableCollection<CareerSummary> Careers { get; }
    public ObservableCollection<CareerTierNodeViewModel> CareerLadderNodes { get; }
    public ObservableCollection<GarageCarCardViewModel> GarageCars { get; }
    public ObservableCollection<GarageCarCardViewModel> OwnedCars { get; }
    public ObservableCollection<GarageCarCardViewModel> BuyableCars { get; }
    public ObservableCollection<GarageCarCardViewModel> RentalCars { get; }
    public ObservableCollection<GarageCarCardViewModel> LockedCars { get; }
    public ObservableCollection<GarageCarCardViewModel> DlcCars { get; }
    public ObservableCollection<GarageClassViewModel> GarageClasses { get; }
    public ObservableCollection<DepartmentUpgradeViewModel> TeamHqDepartments { get; }
    public ObservableCollection<string> UnlockedLeagues { get; }
    public ObservableCollection<string> Challenges { get; }
    public ObservableCollection<RivalSummaryViewModel> Rivals { get; }
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
    public RelayCommand DismissPendingResultCommand => _dismissPendingResultCommand;
    public RelayCommand RefreshCommand => _refreshCommand;
    public RelayCommand ShowRaceDeskPageCommand => _showRaceDeskPageCommand;
    public RelayCommand ShowCareerPageCommand => _showCareerPageCommand;
    public RelayCommand ShowGaragePageCommand => _showGaragePageCommand;
    public RelayCommand ShowTeamHqPageCommand => _showTeamHqPageCommand;
    public RelayCommand ShowRivalsPageCommand => _showRivalsPageCommand;
    public RelayCommand ShowHistoryPageCommand => _showHistoryPageCommand;
    public RelayCommand ShowSettingsPageCommand => _showSettingsPageCommand;
    public RaceDeskViewModel RaceDesk { get; }
    public DiagnosticsSummaryViewModel Diagnostics { get; }
    public ProfileSummaryViewModel Profile { get; } = new();

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

    public GarageCarCardViewModel? SelectedGarageCar
    {
        get => _selectedGarageCar;
        set
        {
            if (SetProperty(ref _selectedGarageCar, value))
            {
                RaisePropertyChanged(nameof(SelectedGarageCarTitle));
                RaisePropertyChanged(nameof(SelectedGarageCarClass));
                RaisePropertyChanged(nameof(SelectedGarageCarStatus));
                RaisePropertyChanged(nameof(SelectedGarageCarCondition));
                RaisePropertyChanged(nameof(SelectedGarageCarSummary));
                RaisePropertyChanged(nameof(SelectedGarageCarActionText));
                RaisePropertyChanged(nameof(SelectedGarageCarUnlockText));
                RaisePropertyChanged(nameof(SelectedGarageCarPriceText));
                RaisePropertyChanged(nameof(SelectedGarageCarEligibleEventsText));
                RaisePropertyChanged(nameof(SelectedGarageCarCareerStartsText));
                RaisePropertyChanged(nameof(CanUseSelectedGarageCar));
                RaisePropertyChanged(nameof(CanRepairSelectedGarageCar));
            }
        }
    }

    public CareerTierNodeViewModel? SelectedCareerTierNode
    {
        get => _selectedCareerTierNode;
        set
        {
            if (SetProperty(ref _selectedCareerTierNode, value))
            {
                RaisePropertyChanged(nameof(SelectedCareerTierTitle));
                RaisePropertyChanged(nameof(SelectedCareerTierStatus));
                RaisePropertyChanged(nameof(SelectedCareerTierDescription));
            }
        }
    }

    public RivalSummaryViewModel? SelectedRival
    {
        get => _selectedRival;
        set
        {
            if (SetProperty(ref _selectedRival, value))
            {
                RaisePropertyChanged(nameof(SelectedRivalName));
                RaisePropertyChanged(nameof(SelectedRivalStatus));
                RaisePropertyChanged(nameof(SelectedRivalHeat));
                RaisePropertyChanged(nameof(SelectedRivalDetail));
                RaisePropertyChanged(nameof(SelectedRivalHeadToHead));
                RaisePropertyChanged(nameof(SelectedRivalRewardPreview));
                RaisePropertyChanged(nameof(SelectedRivalPortraitAssetPath));
            }
        }
    }

    public DepartmentUpgradeViewModel? SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            if (SetProperty(ref _selectedDepartment, value))
            {
                RaisePropertyChanged(nameof(SelectedDepartmentName));
                RaisePropertyChanged(nameof(SelectedDepartmentSummary));
            }
        }
    }

    public DriverPortraitOptionViewModel? SelectedPlayerPortrait
    {
        get => _selectedPlayerPortrait;
        set
        {
            if (SetProperty(ref _selectedPlayerPortrait, value))
            {
                _createCareerCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(SelectedPlayerPortraitAssetPath));
                RaisePropertyChanged(nameof(SelectedPlayerPortraitLabel));
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
                RaceDesk.SelectedSessionPresetSummaryText = SelectedSessionPresetSummaryText;
                RaceDesk.CanDeleteSelectedSessionPreset = CanDeleteSelectedSessionPreset;
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
                RaisePropertyChanged(nameof(SelectedRecentResultStatusLine));
                RaisePropertyChanged(nameof(SelectedRecentResultCompactDetails));
                RaisePropertyChanged(nameof(LastResultPrimaryPositionText));
                RaisePropertyChanged(nameof(LastResultMetaText));
                RaisePropertyChanged(nameof(LastResultOutcomeText));
            }
        }
    }

    public string ConnectionStateText
    {
        get => _connectionStateText;
        private set
        {
            if (SetProperty(ref _connectionStateText, value))
            {
                RaceDesk.ConnectionStateText = value;
            }
        }
    }

    public string SessionSummary
    {
        get => _sessionSummary;
        private set
        {
            if (SetProperty(ref _sessionSummary, value))
            {
                RaceDesk.SessionSummary = value;
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                RaceDesk.StatusMessage = value;
            }
        }
    }

    public string RewardSummary
    {
        get => _rewardSummary;
        private set
        {
            if (SetProperty(ref _rewardSummary, value))
            {
                RaceDesk.RewardSummary = value;
            }
        }
    }

    public string LauncherStatusText
    {
        get => _launcherStatusText;
        private set
        {
            if (SetProperty(ref _launcherStatusText, value))
            {
                RaceDesk.LauncherStatusText = value;
            }
        }
    }

    public string InstallPathText
    {
        get => _installPathText;
        private set
        {
            if (SetProperty(ref _installPathText, value))
            {
                RaceDesk.InstallPathText = value;
            }
        }
    }

    public string ProfileDirectoryText => _sessionPresetService.ProfileDirectory is null
        ? "AMS2 profile directory not detected yet."
        : _sessionPresetService.ProfileDirectory;
    public string ProfileDirectoryStatusText => _sessionPresetService.ProfileDirectory is null
        ? "Profile directory not detected."
        : "Profile directory detected.";

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

    public int SelectedPageIndex
    {
        get => _selectedPageIndex;
        set
        {
            if (SetProperty(ref _selectedPageIndex, value))
            {
                RaisePropertyChanged(nameof(IsRaceDeskPageSelected));
                RaisePropertyChanged(nameof(IsCareerPageSelected));
                RaisePropertyChanged(nameof(IsGaragePageSelected));
                RaisePropertyChanged(nameof(IsTeamHqPageSelected));
                RaisePropertyChanged(nameof(IsRivalsPageSelected));
                RaisePropertyChanged(nameof(IsHistoryPageSelected));
                RaisePropertyChanged(nameof(IsSettingsPageSelected));
            }
        }
    }

    public string DeveloperToolsVisibilityText => DeveloperToolsEnabled ? "Developer tools are visible." : "Developer tools are hidden.";
    public bool IsRaceDeskPageSelected => SelectedPageIndex == 0;
    public bool IsCareerPageSelected => SelectedPageIndex == 1;
    public bool IsGaragePageSelected => SelectedPageIndex == 2;
    public bool IsTeamHqPageSelected => SelectedPageIndex == 3;
    public bool IsRivalsPageSelected => SelectedPageIndex == 4;
    public bool IsHistoryPageSelected => SelectedPageIndex == 5;
    public bool IsSettingsPageSelected => SelectedPageIndex == 6;
    public string AvailableCareersText => Careers.Count == 0 ? "No saved careers yet." : $"{Careers.Count} saved career profile(s).";
    public string RecentResultsHeaderText => RecentResults.Count == 0 ? "No past races logged yet." : $"{RecentResults.Count} logged race result(s).";
    public string RaceHistoryHeaderText => RaceHistory.Count == 0 ? "No season log yet." : $"{RaceHistory.Count} total logged race(s).";
    public string ActiveCareerSummaryText => _career is null
        ? "No active career loaded."
        : $"Active profile: {CurrentCareerName}  |  Level {_career.Progression.Level}  |  {CurrentLeagueName}";
    public string SelectedCareerSummaryText => SelectedCareerSummary is null
        ? "No profile selected."
        : $"Selected profile: {SelectedCareerSummary.Name}  |  Level {SelectedCareerSummary.Level}";
    public string SeasonSummaryText => BuildSeasonSummary();
    public string SessionPresetStatusText => BuildSessionPresetStatusText();
    public string SelectedSessionPresetSummaryText => SelectedSessionPreset is null
        ? "No session preset selected."
        : $"{SelectedSessionPreset.SourceLabel}: {SelectedSessionPreset.Name}\n{SelectedSessionPreset.Description}";
    public bool CanDeleteSelectedSessionPreset => SelectedSessionPreset?.CanDelete == true;

    public string CurrentCareerName => _career is null
        ? "No active career"
        : string.Equals(_career.Name, "New Careersad", StringComparison.OrdinalIgnoreCase)
            ? "New Career"
            : _career.Name;
    public string CurrentStarterCar => _career?.PlayerProfile.StarterCarName ?? "Choose a starter car";
    public string CurrentPlayerPortraitLabel => _career is null
        ? "No portrait selected"
        : _driverPortraitService.ResolvePortrait(_content, _career.PlayerProfile.PortraitId).DisplayLabel;
    public string ActivePlayerPortraitAssetPath => ResolvePortraitAssetPath(_career?.PlayerProfile.PortraitId);
    public string SelectedPlayerPortraitAssetPath => SelectedPlayerPortrait?.AssetPath ?? ResolvePortraitAssetPath(null);
    public string SelectedPlayerPortraitLabel => SelectedPlayerPortrait?.DisplayLabel ?? "Select a portrait";
    public string CurrentTitle => GetCurrentTitle();
    public string CurrentLeagueName => _career is null ? "No active league" : _content.Leagues.First(x => x.Id == _career.ActiveLeagueId).Name;
    public string ProgressText => _career is null
        ? "Level 0"
        : $"Level {_career.Progression.Level}  |  XP {_career.Progression.Xp}  |  Credits {_career.Progression.Credits:n0}";
    public string StandingText => _career is null
        ? "Driver rating not established."
        : $"Driver Rating {_career.Progression.DriverRating:n0}  |  Reputation {_career.Progression.Reputation}";
    public string CareerNameCharacterCount => $"{CareerNameInput.Length}/{CareerNameMaxLength}";
    public string NextEventDisplayTitle => _nextEventPlan is null
        ? "No next event planned."
        : $"{_nextEventPlan.LeagueName} Round {_nextEventPlan.EventNumber}";
    public string NextEventDisplaySubtitle => _nextEventPlan is null
        ? "Create or load a career to generate the next event."
        : $"{_nextEventPlan.LeagueName}  |  Round {_nextEventPlan.EventNumber}/{_nextEventPlan.EventCount}";
    public string NextEventRoundDisplay => _nextEventPlan is null
        ? "-"
        : $"{_nextEventPlan.EventNumber}/{_nextEventPlan.EventCount}";
    public string NextEventSessionDisplay => _nextEventPlan?.EventTemplateName ?? "No session planned";
    public string NextEventRewardCreditsText => _nextEventPlan is null
        ? "No reward set"
        : $"+{_nextEventPlan.BaseCreditReward:n0} CR";
    public string NextEventRewardXpText => _nextEventPlan is null
        ? string.Empty
        : $"+{_nextEventPlan.BaseXpReward} XP";
    public string NextEventHeroMetaText => _nextEventPlan is null
        ? "No event package generated yet."
        : $"{_nextEventPlan.Country}  |  Grid {_nextEventPlan.RecommendedGridSize}  |  Class {_nextEventPlan.PlayerCarClassName}";
    public string NextEventHeroNotesText => _nextEventPlan is null
        ? "Generate an event to receive setup guidance."
        : _nextEventPlan.SetupNotes;
    public string NextEventOverlapGuidanceText => _nextEventPlan is null
        ? "No overlap event is planned yet."
        : $"{_nextEventPlan.TrackDisplayName}  |  Grid {_nextEventPlan.RecommendedGridSize}  |  {_nextEventPlan.PlayerCarClassName}";
    public string CurrentLeagueDescriptionText => _career is null
        ? "Create a career to begin the motorsport ladder."
        : $"Complete Rookie events, raise your rating, and unlock the next branch beyond {CurrentLeagueName}.";
    public string CareerProgressSummaryText => _career is null
        ? "No progress recorded yet."
        : $"Level {_career.Progression.Level}  |  Driver Rating {_career.Progression.DriverRating:n0}  |  Credits {_career.Progression.Credits:n0}  |  Reputation {_career.Progression.Reputation}";
    public string ChallengeSummaryText => Challenges.Count == 0
        ? "No active challenge summary yet."
        : Challenges.First();
    public string GarageEligibleEventText => _nextEventPlan is null
        ? "No eligible event planned yet."
        : $"{NextEventDisplayTitle}  |  {NextEventHeroMetaText}";
    public string GarageDetailSummaryText => SelectedGarageCar?.SummaryText
        ?? (_career is null ? "No active car profile." : $"{CurrentStarterCar}  |  {CurrentLeagueName}  |  {CurrentTitle}");
    public string SelectedGarageCarTitle => SelectedGarageCar?.Name ?? "Select a car";
    public string SelectedGarageCarClass => SelectedGarageCar is null ? "No class selected" : SelectedGarageCar.ClassName;
    public string SelectedGarageCarStatus => SelectedGarageCar?.StatusText ?? "No car selected";
    public string SelectedGarageCarCondition => SelectedGarageCar?.ConditionText ?? "Condition unavailable";
    public string SelectedGarageCarSummary => SelectedGarageCar?.SummaryText ?? "Select a car to see details.";
    public string SelectedGarageCarActionText => SelectedGarageCar?.ActionText ?? "Select a car";
    public string SelectedGarageCarUnlockText => SelectedGarageCar?.UnlockText ?? string.Empty;
    public string SelectedGarageCarPriceText => SelectedGarageCar?.PriceText ?? string.Empty;
    public string SelectedGarageCarEligibleEventsText => SelectedGarageCar?.EligibleEventsText ?? string.Empty;
    public string SelectedGarageCarCareerStartsText => SelectedGarageCar?.CareerStartsText ?? string.Empty;
    public bool CanUseSelectedGarageCar => SelectedGarageCar?.CanAct == true;
    public bool CanRepairSelectedGarageCar => SelectedGarageCar?.CanAct == true && SelectedGarageCar?.ActionText.Contains("Repair", StringComparison.OrdinalIgnoreCase) == true;

    public string SelectedCareerTierTitle => SelectedCareerTierNode?.TierName ?? "Select a tier";
    public string SelectedCareerTierStatus => SelectedCareerTierNode?.StatusText ?? string.Empty;
    public string SelectedCareerTierDescription => SelectedCareerTierNode?.Description ?? string.Empty;

    public string FeaturedRivalName => _career?.Rivals.OrderByDescending(x => x.RivalryIntensity).FirstOrDefault()?.Name ?? "No featured rival";
    public string FeaturedRivalPortraitAssetPath => ResolvePortraitAssetPath(_career?.Rivals.OrderByDescending(x => x.RivalryIntensity).FirstOrDefault()?.PortraitId);
    public string FeaturedRivalStatusText
    {
        get
        {
            var rival = _career?.Rivals.OrderByDescending(x => x.RivalryIntensity).FirstOrDefault();
            return rival is null ? "No active rival" : rival.RivalryIntensity >= 25 ? "Active Rival" : "Emerging Rival";
        }
    }
    public string FeaturedRivalHeatText
    {
        get
        {
            var rival = _career?.Rivals.OrderByDescending(x => x.RivalryIntensity).FirstOrDefault();
            return rival is null ? "0%" : $"{Math.Clamp(rival.RivalryIntensity, 0, 100)}%";
        }
    }
    public string FeaturedRivalMetaText
    {
        get
        {
            var rival = _career?.Rivals.OrderByDescending(x => x.RivalryIntensity).FirstOrDefault();
            return rival is null
                ? "Keep racing to surface more rivals."
                : $"{rival.Specialty}  |  Rating {rival.DriverRating:n0}  |  Rivalry {rival.RivalryIntensity}";
        }
    }
    public string FeaturedRivalHeadToHeadText => BuildRivalHeadToHeadText(_career?.Rivals.OrderByDescending(x => x.RivalryIntensity).FirstOrDefault());
    public string FeaturedRivalDetailText => _career?.Rivals.Count > 0
        ? "This is your most relevant current competitor. Keep racing and the rivalry board will sharpen around this record."
        : "No rival has emerged yet. Keep racing to build recurring competitors and a stronger rivalry board.";
    public string SelectedRivalName => SelectedRival?.Name ?? "Select a rival";
    public string SelectedRivalStatus => SelectedRival?.StatusText ?? string.Empty;
    public string SelectedRivalHeat => SelectedRival?.HeatText ?? string.Empty;
    public string SelectedRivalDetail => SelectedRival?.DetailText ?? string.Empty;
    public string SelectedRivalHeadToHead => SelectedRival?.HeadToHeadText ?? string.Empty;
    public string SelectedRivalRewardPreview => SelectedRival?.RewardPreviewText ?? string.Empty;
    public string SelectedRivalPortraitAssetPath => SelectedRival?.PortraitAssetPath ?? ResolvePortraitAssetPath(null);

    public string SelectedDepartmentName => SelectedDepartment?.DepartmentName ?? "Select a department";
    public string SelectedDepartmentSummary => SelectedDepartment?.SummaryText ?? string.Empty;
    public string TeamHqSummaryText => _career is null
        ? "No operation summary available yet."
        : $"Current focus: {_career.Progression.Level switch { >= 10 => "Prestige build", >= 5 => "Club growth", _ => "Rookie efficiency" }}";
    public string TeamHqEfficiencyText => _career is null
        ? "Efficiency metrics unavailable."
        : $"Driver Rating {_career.Progression.DriverRating:n0}  |  Credits {_career.Progression.Credits:n0}  |  Reputation {_career.Progression.Reputation}";
    public string TeamHqNextUnlockText => _career is null
        ? "Unlocks unavailable."
        : _career.Progression.Level >= 5 ? "Logistics, Simulator, and Media Office unlock next." : "Requires Club Tier.";
    public string GarageEmptyStateTitle => _career is null ? "No garage available yet" : "No owned cars yet";
    public string GarageEmptyStateText => _career is null
        ? "Create a career to unlock your first car profile."
        : "Owned cars appear here as soon as your career has one.";
    public bool HasOwnedCars => OwnedCars.Count > 0;
    public string TeamHqEmptyStateTitle => _career is null ? "No departments unlocked yet" : "Departments still locked";
    public string TeamHqEmptyStateText => _career is null
        ? "Create a career to begin unlocking permanent team upgrades."
        : "Reach Club Tier to open the remaining departments.";
    public bool HasTeamHqDepartments => TeamHqDepartments.Count > 0;

    public Brush ConnectionStateBrush => _telemetryFeed.ConnectionState switch
    {
        TelemetryConnectionState.Monitoring => new SolidColorBrush(Color.FromRgb(103, 209, 59)),
        TelemetryConnectionState.Simulating => new SolidColorBrush(Color.FromRgb(255, 181, 69)),
        _ => _launchService.IsGameRunning ? new SolidColorBrush(Color.FromRgb(255, 181, 69)) : new SolidColorBrush(Color.FromRgb(115, 129, 145))
    };

    public Brush TelemetryHealthBrush => _telemetryFeed.ConnectionState switch
    {
        TelemetryConnectionState.Monitoring => new SolidColorBrush(Color.FromRgb(103, 209, 59)),
        TelemetryConnectionState.Simulating => new SolidColorBrush(Color.FromRgb(255, 181, 69)),
        _ => _launchService.IsGameRunning ? new SolidColorBrush(Color.FromRgb(255, 181, 69)) : new SolidColorBrush(Color.FromRgb(115, 129, 145))
    };

    public string TelemetryHealthText => _telemetryFeed.ConnectionState switch
    {
        TelemetryConnectionState.Monitoring => "Telemetry: Healthy",
        TelemetryConnectionState.Simulating => "Telemetry: Test",
        _ => _launchService.IsGameRunning ? "Telemetry: Waiting" : "Telemetry: Offline"
    };

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
                _dismissPendingResultCommand.RaiseCanExecuteChanged();

                if (value is null)
                {
                    RaceDesk.ResultReview.Clear();
                }
                else
                {
                    RaceDesk.ResultReview.SetPendingDraft(value);
                }
            }
        }
    }

    public bool HasPendingDraft => PendingDraft is not null;
    public string PendingDraftSummary => PendingDraft?.Summary ?? "No pending result review.";
    public string SelectedRecentResultHeadline => SelectedRecentResult?.Headline ?? "Manual Review Needed";
    public string SelectedRecentResultDetails => SelectedRecentResult?.Details ?? "No confirmed result yet.";
    public string SelectedRecentResultStatusLine => SelectedRecentResult is null
        ? "Manual review needed | Result could not be confirmed automatically"
        : $"{SelectedRecentResult.ResultTag}  |  {SelectedRecentResult.CleanlinessTag}  |  {SelectedRecentResult.ReviewTag}";
    public string SelectedRecentResultCompactDetails => SelectedRecentResult is null
        ? "No result selected."
        : $"Overall P{SelectedRecentResult.OverallPosition}/{SelectedRecentResult.Entrants}  |  Class P{SelectedRecentResult.ClassPosition}  |  {SelectedRecentResult.LapsCompleted} laps";
    public string LastResultPrimaryPositionText => SelectedRecentResult is null ? "—" : $"P{SelectedRecentResult.OverallPosition}";
    public string LastResultMetaText => SelectedRecentResult?.Subtitle ?? "Result could not be confirmed automatically.";
    public string LastResultOutcomeText => SelectedRecentResult?.ResultTag ?? "Manual Review Needed";

    public async Task InitializeAsync()
    {
        SelectedStarterCar = StarterCars.FirstOrDefault();
        SelectedPlayerPortrait = PlayerPortraitOptions.FirstOrDefault();
        _career = await _repository.LoadCurrentCareerAsync();
        await EnsureCareerPortraitsAsync();
        RestoreDiagnosticsSnapshot();

        await _telemetryFeed.StartAsync();
        await RefreshAsync();
        UpdateRaceDeskFlowState();
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
            StatusMessage = "Create a new career from Career, then prepare your next event from Race Desk.";
            RefreshCollections(Array.Empty<RaceResultConfirmed>());
            RaiseCareerProperties();
            UpdateRaceDeskFlowState();
            RaiseCommandStates();
            return;
        }

        var results = await _repository.LoadRaceHistoryAsync(_career.Id);
        RefreshCollections(results);
        RaiseCareerProperties();
        RefreshNextEventPlan(results);
        StatusMessage = $"Active league: {CurrentLeagueName}. {NextEventHeadlineText}";
        UpdateRaceDeskFlowState();
        RaiseCommandStates();
    }

    private Task SetSelectedPageAsync(int pageIndex)
    {
        SelectedPageIndex = pageIndex;
        return Task.CompletedTask;
    }

    private async Task CreateCareerAsync()
    {
        if (SelectedStarterCar is null)
        {
            return;
        }

        _career = _careerFactory.CreateNewCareer(CareerNameInput, SelectedStarterCar, CareerPreset.Standard, _content, SelectedPlayerPortrait?.Id);
        await _repository.SaveCareerAsync(_career, setAsCurrent: true);
        RewardSummary = "Career created. Your first objective is to complete the Rookie Cup.";
        StatusMessage = "Career created. Go to Race Desk to prepare the next event.";
        await RefreshAsync();
        UpdateRaceDeskFlowState();
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
        Diagnostics.RecordExportStatus(NextEventExportStatusText);
        StatusMessage = result.Message;

        if (result.Success && result.Preset is not null)
        {
            SelectedSessionPreset = SessionPresets.FirstOrDefault(x => x.Key == result.Preset.Key) ?? result.Preset;
        }

        UpdateRaceDeskFlowState();
        await Task.CompletedTask;
    }

    private async Task ApplyRecommendedEventAndLaunchAsync()
    {
        var (applyResult, launchResult) = _raceAutomationCoordinator.PrepareAndLaunch(_nextEventPlan, vr: false);
        NextEventExportStatusText = BuildExportStatusText(applyResult.Message, applyResult.Readiness, applyResult.RequiresGameRestart);
        Diagnostics.RecordExportStatus(NextEventExportStatusText);

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
        UpdateRaceDeskFlowState();
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
        UpdateRaceDeskFlowState();
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
        await EnsureCareerPortraitsAsync();
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
        await EnsureCareerPortraitsAsync();
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
            ? "Result simulation: manual review likely needed."
            : "Result simulation: clean path.";

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

    private async Task DismissPendingResultAsync()
    {
        if (PendingDraft is null)
        {
            return;
        }

        Diagnostics.RecordResultStatus(PendingDraft.AutomationRunId, $"Pending result dismissed: {PendingDraft.Outcome} at {PendingDraft.TrackName}.");
        StatusMessage = "Pending result review dismissed.";
        PendingDraft = null;
        UpdateRaceDeskFlowState();
        RaiseCommandStates();
        await Task.CompletedTask;
    }

    private void OnSessionStatusChanged(object? sender, SessionStatusSnapshot snapshot)
    {
        _ = Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _automationTraceWriter.AppendSessionSnapshot(_raceAutomationCoordinator.CurrentStatus.RunId, snapshot);
            var leagueName = ResolveSessionLeagueName(snapshot);
            var trackName = ResolveSessionTrackName(snapshot);

            ConnectionStateText = BuildConnectionStateText();
            RaisePropertyChanged(nameof(TelemetryHealthText));
            SessionSummary = snapshot.SessionPhase switch
            {
                SessionPhase.Grid => $"Grid formed for {leagueName} at {trackName}.",
                SessionPhase.Running => $"Race running: {leagueName} at {trackName}.",
                SessionPhase.Finished => $"Session finished at {trackName}. Evaluating result confidence.",
                _ => snapshot.IsConnected ? "Monitoring AMS2 for the next session." : trackName
            };
            Diagnostics.RecordSessionStatus(_raceAutomationCoordinator.CurrentStatus.RunId, SessionSummary);
            UpdateRaceDeskFlowState();
        });
    }

    private async void OnDraftCreated(object? sender, RaceResultDraft draft)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            _automationTraceWriter.AppendDraftCreated(draft);
            if (draft.Confidence == ResultConfidence.High)
            {
                await CommitDraftAsync(draft, reviewed: false);
                return;
            }

            PendingDraft = draft;
            StatusMessage = "Result confidence is below automatic commit threshold. Review and confirm the detected finish.";
            Diagnostics.RecordResultStatus(draft.AutomationRunId, $"Pending manual review for {draft.Outcome} at {draft.TrackName}.");
            UpdateRaceDeskFlowState();
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
            RaceDesk.Automation.Update(status);
            Diagnostics.RecordAutomationStatus(status.RunId, $"{status.Stage}: {status.Headline}");

            if (status.Stage == RaceAutomationStage.SessionFinished)
            {
                StatusMessage = status.Headline;
            }

            UpdateRaceDeskFlowState();
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
        Diagnostics.RecordResultStatus(confirmed.Draft.AutomationRunId, $"Committed {confirmed.Draft.Outcome} result for {confirmed.Draft.TrackName}.");

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
        UpdateRaceDeskFlowState();
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
            RefreshPresentationCollectionsClean();
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
            Rivals.Add(new RivalSummaryViewModel
            {
                Name = rival.Name,
                Summary = $"{rival.Specialty}  |  Rating {rival.DriverRating:n0}  |  Rivalry {rival.RivalryIntensity}",
                PortraitAssetPath = ResolvePortraitAssetPath(rival.PortraitId),
                HeadToHeadText = BuildRivalHeadToHeadText(rival)
            });
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
        RefreshPresentationCollectionsClean();
        RefreshNextEventPlan(results);
        RaisePropertyChanged(nameof(RecentResultsHeaderText));
        RaisePropertyChanged(nameof(RaceHistoryHeaderText));
        RaisePropertyChanged(nameof(SeasonSummaryText));
        RaisePropertyChanged(nameof(ChallengeSummaryText));
        UpdateRaceDeskFlowState();
    }

    private void RefreshPresentationCollections()
    {
        CareerLadderNodes.Clear();
        GarageCars.Clear();
        OwnedCars.Clear();
        BuyableCars.Clear();
        RentalCars.Clear();
        LockedCars.Clear();
        DlcCars.Clear();
        GarageClasses.Clear();
        TeamHqDepartments.Clear();

        var currentLeagueId = _career?.ActiveLeagueId;
        var completedLeagueIds = _career is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : _career.CompletedLeagueIds.ToHashSet(StringComparer.Ordinal);
        var unlockedLeagueIds = _career is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : _career.UnlockedLeagueIds.ToHashSet(StringComparer.Ordinal);

        foreach (var league in _content.Leagues.OrderBy(league => league.RequiredLevel))
        {
            var isCurrent = string.Equals(league.Id, currentLeagueId, StringComparison.OrdinalIgnoreCase);
            var isCompleted = completedLeagueIds.Contains(league.Id);
            var isUnlocked = unlockedLeagueIds.Contains(league.Id) || isCurrent || isCompleted;

            CareerLadderNodes.Add(new CareerTierNodeViewModel
            {
                TierName = $"Tier {GetLeagueTierIndex(league)}",
                SeriesName = league.Name,
                StatusText = isCurrent ? "Active" : isCompleted ? "Completed" : isUnlocked ? "Available" : "Locked",
                Description = isCurrent
                    ? $"{league.ClassName} is your active progression branch."
                    : isCompleted
                        ? $"You cleared {league.Name}."
                        : isUnlocked
                            ? $"{league.Name} is ready when you choose it."
                            : GetLeagueLockedText(league),
                IsCurrent = isCurrent,
                IsCompleted = isCompleted,
                IsLocked = !isUnlocked
            });
        }

        foreach (var car in _content.Cars)
        {
            var classDef = _content.CarClasses.FirstOrDefault(x => x.Id == car.ClassId);
            var owned = _career is not null && string.Equals(_career.PlayerProfile.StarterCarId, _content.StarterCars.FirstOrDefault(x => x.CarId == car.Id)?.Id, StringComparison.OrdinalIgnoreCase);
            var unlocked = classDef?.IsStarterEligible == true
                || (_career is not null && _career.UnlockedLeagueIds.Any(id => string.Equals(id, _career.ActiveLeagueId, StringComparison.Ordinal)));
            var locked = classDef?.IsDlc == true;
            var status = owned ? "Owned" : locked ? "Locked" : unlocked ? "Available" : "Rental";
            var action = owned ? "Use For Eligible Event" : locked ? "Locked Until Content Is Available" : unlocked ? "Buy Car" : "Rent For Event";
            var card = new GarageCarCardViewModel
            {
                Name = car.Name,
                ClassName = classDef?.Name ?? car.ClassId,
                StatusText = status,
                ConditionText = owned ? "Condition: Good" : locked ? "Condition: Locked" : "Condition: Fresh",
                EligibleEventsText = ResolveEligibleEventsText(car.ClassId),
                CareerStartsText = owned ? "Career starts: 1" : "Career starts: 0",
                PriceText = locked ? "Unlock with DLC content" : owned ? "Owned at start" : $"Price {BuildCarPrice(car):n0} credits",
                UnlockText = locked ? "Unlock requirement: DLC content" : unlocked ? "Unlock requirement: None" : "Unlock requirement: Career progress",
                ActionText = action,
                SummaryText = $"{car.Manufacturer}  |  {classDef?.Family ?? "Car"}  |  {status}",
                CanAct = !locked,
                IsLocked = locked,
                IsOwned = owned
            };

            GarageCars.Add(card);
            if (owned)
            {
                OwnedCars.Add(card);
            }
            else if (locked)
            {
                LockedCars.Add(card);
                DlcCars.Add(card);
            }
            else if (unlocked)
            {
                BuyableCars.Add(card);
            }
            else
            {
                RentalCars.Add(card);
            }
        }

        foreach (var classDef in _content.CarClasses)
        {
            GarageClasses.Add(new GarageClassViewModel
            {
                Name = classDef.Name,
                Family = classDef.Family,
                Tier = classDef.Tier,
                StatusText = classDef.IsDlc ? "DLC" : classDef.IsStarterEligible ? "Starter eligible" : "Available",
                Description = classDef.Description,
                IsDlc = classDef.IsDlc,
                IsStarterEligible = classDef.IsStarterEligible
            });
        }

        var departmentData = new[]
        {
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Mechanic",
                IconGlyph = "⚙",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Repair cost reduction",
                NextBonusText = "Quick repair discount",
                UpgradeCostText = "18,000 credits",
                StatusText = _career?.Progression.Credits >= 18000 ? "Ready" : "Locked",
                SummaryText = "Permanent repair savings and faster turnaround for damaged cars.",
                CanUpgrade = _career?.Progression.Credits >= 18000 == true,
                IsLocked = false
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Race Engineer",
                IconGlyph = "∿",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Setup notes",
                NextBonusText = "Practice XP bonus",
                UpgradeCostText = "22,000 credits",
                StatusText = _career?.Progression.Level >= 2 ? "Ready" : "Locked",
                SummaryText = "Sharper event prep and better support for learning each new track.",
                CanUpgrade = _career?.Progression.Credits >= 22000 == true && _career.Progression.Level >= 2,
                IsLocked = _career?.Progression.Level < 2
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Manager",
                IconGlyph = "¤",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Credit rewards",
                NextBonusText = "Entry fee reduction",
                UpgradeCostText = "24,000 credits",
                StatusText = _career?.Progression.Level >= 3 ? "Ready" : "Locked",
                SummaryText = "Improves the business side of the career and reduces event overhead.",
                CanUpgrade = _career?.Progression.Credits >= 24000 == true && _career.Progression.Level >= 3,
                IsLocked = _career?.Progression.Level < 3
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Logistics",
                IconGlyph = "⇄",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Rental support",
                NextBonusText = "Insurance benefits",
                UpgradeCostText = "26,000 credits",
                StatusText = _career?.Progression.Level >= 4 ? "Ready" : "Locked",
                SummaryText = "Helps manage travel, rentals, and event cancellation friction.",
                CanUpgrade = _career?.Progression.Credits >= 26000 == true && _career.Progression.Level >= 4,
                IsLocked = _career?.Progression.Level < 4
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Simulator",
                IconGlyph = "◫",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Track familiarity",
                NextBonusText = "Prep bonus",
                UpgradeCostText = "20,000 credits",
                StatusText = _career?.Progression.Level >= 2 ? "Ready" : "Locked",
                SummaryText = "Builds confidence before race day and supports cleaner progression.",
                CanUpgrade = _career?.Progression.Credits >= 20000 == true && _career.Progression.Level >= 2,
                IsLocked = _career?.Progression.Level < 2
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Media Office",
                IconGlyph = "★",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Reputation gain",
                NextBonusText = "Prestige invitations",
                UpgradeCostText = "28,000 credits",
                StatusText = _career?.Progression.Level >= 5 ? "Ready" : "Locked",
                SummaryText = "Boosts reputation-focused career growth and late-ladder opportunities.",
                CanUpgrade = _career?.Progression.Credits >= 28000 == true && _career.Progression.Level >= 5,
                IsLocked = _career?.Progression.Level < 5
            }
        };

        foreach (var department in departmentData)
        {
            TeamHqDepartments.Add(department);
        }

        foreach (var rival in _career is null
                     ? Array.Empty<RivalProfile>()
                     : _career.Rivals.OrderByDescending(x => x.RivalryIntensity).ToArray())
        {
            var status = rival.RivalryIntensity >= 25 ? "Active" : rival.RivalryIntensity >= 15 ? "Emerging" : "Potential";
            Rivals.Add(new RivalSummaryViewModel
            {
                Name = rival.Name,
                Summary = $"{rival.Personality}  |  {rival.Specialty}  |  Rating {rival.DriverRating:n0}",
                PortraitAssetPath = ResolvePortraitAssetPath(rival.PortraitId),
                StatusText = status,
                HeatText = $"{Math.Clamp(rival.RivalryIntensity, 0, 100)} heat",
                DetailText = $"Rival heat is {rival.RivalryIntensity}. This is presentation data only.",
                HeadToHeadText = "Last 5 head-to-head: unavailable",
                RewardPreviewText = "Rivalry rewards will appear as the board grows."
            });
        }

        SelectedCareerTierNode = CareerLadderNodes.FirstOrDefault(node => node.IsCurrent)
            ?? CareerLadderNodes.FirstOrDefault(node => !node.IsLocked)
            ?? CareerLadderNodes.FirstOrDefault();
        SelectedGarageCar = GarageCars.FirstOrDefault(card => card.IsOwned)
            ?? GarageCars.FirstOrDefault(card => !card.IsLocked)
            ?? GarageCars.FirstOrDefault();
        SelectedDepartment = TeamHqDepartments.FirstOrDefault(card => card.CanUpgrade)
            ?? TeamHqDepartments.FirstOrDefault();
        SelectedRival = Rivals.FirstOrDefault();

        RaisePropertyChanged(nameof(GarageEmptyStateTitle));
        RaisePropertyChanged(nameof(GarageEmptyStateText));
        RaisePropertyChanged(nameof(HasOwnedCars));
        RaisePropertyChanged(nameof(TeamHqEmptyStateTitle));
        RaisePropertyChanged(nameof(TeamHqEmptyStateText));
        RaisePropertyChanged(nameof(HasTeamHqDepartments));
    }

    private void RefreshPresentationCollectionsClean()
    {
        CareerLadderNodes.Clear();
        GarageCars.Clear();
        OwnedCars.Clear();
        BuyableCars.Clear();
        RentalCars.Clear();
        LockedCars.Clear();
        DlcCars.Clear();
        GarageClasses.Clear();
        TeamHqDepartments.Clear();
        Rivals.Clear();

        var currentLeagueId = _career?.ActiveLeagueId;
        var completedLeagueIds = _career is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : _career.CompletedLeagueIds.ToHashSet(StringComparer.Ordinal);
        var unlockedLeagueIds = _career is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : _career.UnlockedLeagueIds.ToHashSet(StringComparer.Ordinal);

        foreach (var league in _content.Leagues.OrderBy(league => league.RequiredLevel))
        {
            var isCurrent = string.Equals(league.Id, currentLeagueId, StringComparison.OrdinalIgnoreCase);
            var isCompleted = completedLeagueIds.Contains(league.Id);
            var isUnlocked = unlockedLeagueIds.Contains(league.Id) || isCurrent || isCompleted;

            CareerLadderNodes.Add(new CareerTierNodeViewModel
            {
                TierName = $"Tier {GetLeagueTierIndex(league)}",
                SeriesName = league.Name,
                StatusText = isCurrent ? "Active" : isCompleted ? "Completed" : isUnlocked ? "Available" : "Locked",
                Description = isCurrent
                    ? $"{league.ClassName} is your active progression branch."
                    : isCompleted
                        ? $"You cleared {league.Name}."
                        : isUnlocked
                            ? $"{league.Name} is ready when you choose it."
                            : GetLeagueLockedText(league),
                IsCurrent = isCurrent,
                IsCompleted = isCompleted,
                IsLocked = !isUnlocked
            });
        }

        foreach (var car in _content.Cars)
        {
            var classDef = _content.CarClasses.FirstOrDefault(x => x.Id == car.ClassId);
            var owned = _career is not null && string.Equals(_career.PlayerProfile.StarterCarId, _content.StarterCars.FirstOrDefault(x => x.CarId == car.Id)?.Id, StringComparison.OrdinalIgnoreCase);
            var unlocked = classDef?.IsStarterEligible == true
                || (_career is not null && _career.UnlockedLeagueIds.Any(id => string.Equals(id, _career.ActiveLeagueId, StringComparison.Ordinal)));
            var locked = classDef?.IsDlc == true;
            var status = owned ? "Owned" : locked ? "Locked" : unlocked ? "Available" : "Rental";
            var action = owned ? "Use For Eligible Event" : locked ? "Locked Until Content Is Available" : unlocked ? "Buy Car" : "Rent For Event";
            var card = new GarageCarCardViewModel
            {
                Name = car.Name,
                ClassName = classDef?.Name ?? car.ClassId,
                StatusText = status,
                ConditionText = owned ? "Condition: Good" : locked ? "Condition: Locked" : "Condition: Fresh",
                EligibleEventsText = ResolveEligibleEventsText(car.ClassId),
                CareerStartsText = owned ? "Career starts: 1" : "Career starts: 0",
                PriceText = locked ? "Unlock with DLC content" : owned ? "Owned at start" : $"Price {BuildCarPrice(car):n0} credits",
                UnlockText = locked ? "Unlock requirement: DLC content" : unlocked ? "Unlock requirement: None" : "Unlock requirement: Career progress",
                ActionText = action,
                SummaryText = $"{car.Manufacturer}  |  {classDef?.Family ?? "Car"}  |  {status}",
                CanAct = !locked,
                IsLocked = locked,
                IsOwned = owned
            };

            GarageCars.Add(card);
            if (owned)
            {
                OwnedCars.Add(card);
            }
            else if (locked)
            {
                LockedCars.Add(card);
                DlcCars.Add(card);
            }
            else if (unlocked)
            {
                BuyableCars.Add(card);
            }
            else
            {
                RentalCars.Add(card);
            }
        }

        foreach (var classDef in _content.CarClasses)
        {
            GarageClasses.Add(new GarageClassViewModel
            {
                Name = classDef.Name,
                Family = classDef.Family,
                Tier = classDef.Tier,
                StatusText = classDef.IsDlc ? "DLC" : classDef.IsStarterEligible ? "Starter eligible" : "Available",
                Description = classDef.Description,
                IsDlc = classDef.IsDlc,
                IsStarterEligible = classDef.IsStarterEligible
            });
        }

        var departmentData = new[]
        {
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Mechanic",
                IconGlyph = "⚙",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Repair cost reduction",
                NextBonusText = "Quick repair discount",
                UpgradeCostText = "18,000 credits",
                StatusText = BuildDepartmentStatusText(1, 18000),
                RequirementText = BuildDepartmentRequirementText(1, 18000),
                SummaryText = "Permanent repair savings and faster turnaround for damaged cars.",
                CanUpgrade = _career is not null && _career.Progression.Level >= 1 && _career.Progression.Credits >= 18000,
                IsLocked = _career is null || _career.Progression.Level < 1
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Race Engineer",
                IconGlyph = "∿",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Setup notes",
                NextBonusText = "Practice XP bonus",
                UpgradeCostText = "22,000 credits",
                StatusText = BuildDepartmentStatusText(2, 22000),
                RequirementText = BuildDepartmentRequirementText(2, 22000),
                SummaryText = "Sharper event prep and better support for learning each new track.",
                CanUpgrade = _career is not null && _career.Progression.Level >= 2 && _career.Progression.Credits >= 22000,
                IsLocked = _career is null || _career.Progression.Level < 2
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Manager",
                IconGlyph = "¤",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Credit rewards",
                NextBonusText = "Entry fee reduction",
                UpgradeCostText = "24,000 credits",
                StatusText = BuildDepartmentStatusText(3, 24000),
                RequirementText = BuildDepartmentRequirementText(3, 24000),
                SummaryText = "Improves the business side of the career and reduces event overhead.",
                CanUpgrade = _career is not null && _career.Progression.Level >= 3 && _career.Progression.Credits >= 24000,
                IsLocked = _career is null || _career.Progression.Level < 3
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Logistics",
                IconGlyph = "⇄",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Rental support",
                NextBonusText = "Insurance benefits",
                UpgradeCostText = "26,000 credits",
                StatusText = BuildDepartmentStatusText(5, 26000),
                RequirementText = BuildDepartmentRequirementText(5, 26000),
                SummaryText = "Helps manage travel, rentals, and event cancellation friction.",
                CanUpgrade = _career is not null && _career.Progression.Level >= 5 && _career.Progression.Credits >= 26000,
                IsLocked = _career is null || _career.Progression.Level < 5
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Simulator",
                IconGlyph = "◌",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Track familiarity",
                NextBonusText = "Prep bonus",
                UpgradeCostText = "20,000 credits",
                StatusText = BuildDepartmentStatusText(5, 20000),
                RequirementText = BuildDepartmentRequirementText(5, 20000),
                SummaryText = "Builds confidence before race day and supports cleaner progression.",
                CanUpgrade = _career is not null && _career.Progression.Level >= 5 && _career.Progression.Credits >= 20000,
                IsLocked = _career is null || _career.Progression.Level < 5
            },
            new DepartmentUpgradeViewModel
            {
                DepartmentName = "Media Office",
                IconGlyph = "★",
                CurrentLevelText = "Level 1",
                CurrentBonusText = "Reputation gain",
                NextBonusText = "Prestige invitations",
                UpgradeCostText = "28,000 credits",
                StatusText = BuildDepartmentStatusText(5, 28000),
                RequirementText = BuildDepartmentRequirementText(5, 28000),
                SummaryText = "Boosts reputation-focused career growth and late-ladder opportunities.",
                CanUpgrade = _career is not null && _career.Progression.Level >= 5 && _career.Progression.Credits >= 28000,
                IsLocked = _career is null || _career.Progression.Level < 5
            }
        };

        foreach (var department in departmentData)
        {
            TeamHqDepartments.Add(department);
        }

        foreach (var rival in _career is null
                     ? Array.Empty<RivalProfile>()
                     : _career.Rivals.OrderByDescending(x => x.RivalryIntensity).ToArray())
        {
            var status = rival.RivalryIntensity >= 25 ? "Active" : rival.RivalryIntensity >= 15 ? "Emerging" : "Potential";
            Rivals.Add(new RivalSummaryViewModel
            {
                Name = rival.Name,
                Summary = $"{rival.Personality}  |  {rival.Specialty}  |  Rating {rival.DriverRating:n0}",
                PortraitAssetPath = ResolvePortraitAssetPath(rival.PortraitId),
                StatusText = status,
                HeatText = $"{Math.Clamp(rival.RivalryIntensity, 0, 100)} heat",
                DetailText = $"Heat level {rival.RivalryIntensity}. Keep racing this branch to build a stronger rivalry record.",
                HeadToHeadText = BuildRivalHeadToHeadText(rival),
                RewardPreviewText = "Showdown rewards will appear when this rivalry branch is reached."
            });
        }

        SelectedCareerTierNode = CareerLadderNodes.FirstOrDefault(node => node.IsCurrent)
            ?? CareerLadderNodes.FirstOrDefault(node => !node.IsLocked)
            ?? CareerLadderNodes.FirstOrDefault();
        SelectedGarageCar = GarageCars.FirstOrDefault(card => card.IsOwned)
            ?? GarageCars.FirstOrDefault(card => !card.IsLocked)
            ?? GarageCars.FirstOrDefault();
        SelectedDepartment = TeamHqDepartments.FirstOrDefault(card => card.CanUpgrade)
            ?? TeamHqDepartments.FirstOrDefault();
        SelectedRival = Rivals.FirstOrDefault();
    }

    private static int GetLeagueTierIndex(LeagueDefinition league)
    {
        return league.RequiredLevel switch
        {
            <= 1 => 0,
            <= 2 => 1,
            <= 4 => 2,
            <= 6 => 3,
            <= 8 => 4,
            _ => 5
        };
    }

    private string GetLeagueLockedText(LeagueDefinition league)
    {
        if (_career is null)
        {
            return "Start a career to unlock progression.";
        }

        if (_career.Progression.Level < league.RequiredLevel)
        {
            return $"Reach level {league.RequiredLevel} to unlock this branch.";
        }

        return "Complete prerequisite branches to unlock this series.";
    }

    private string ResolveEligibleEventsText(string classId)
    {
        var events = _content.EventTemplates.Where(template => template.EligibleCarClassIds.Contains(classId, StringComparer.Ordinal)).Select(template => template.Name).Take(2).ToArray();
        return events.Length == 0 ? "Eligible events appear once the branch unlocks." : $"Eligible events: {string.Join(", ", events)}";
    }

    private static string BuildRivalHeadToHeadText(RivalProfile? rival)
    {
        if (rival is null || rival.RecentEncounters.Count == 0)
        {
            return "Last 5 head-to-head: not yet tracked";
        }

        var recent = rival.RecentEncounters.Take(5).ToArray();
        var wins = recent.Count(encounter => encounter.PlayerFinishedAhead);
        var losses = recent.Length - wins;
        var averageGap = recent.Average(encounter => Math.Abs(encounter.RivalPosition - encounter.PlayerPosition));
        var latest = recent[0];

        return $"Last {recent.Length} head-to-head: {wins} wins | {losses} losses | avg gap P{averageGap:0.0} | Latest {latest.Summary}";
    }

    private int BuildCarPrice(OfficialCarDefinition car)
    {
        return Math.Max(4000, 8000 + (car.Name.Length * 450));
    }

    private string BuildDepartmentStatusText(int requiredLevel, int cost)
    {
        if (_career is null)
        {
            return "Locked";
        }

        if (_career.Progression.Level < requiredLevel)
        {
            return "Requires Club Tier";
        }

        var shortfall = cost - _career.Progression.Credits;
        return shortfall > 0 ? $"Need {shortfall:n0} more credits" : "Ready";
    }

    private string BuildDepartmentRequirementText(int requiredLevel, int cost)
    {
        if (_career is null)
        {
            return "Create a career to unlock departments.";
        }

        if (_career.Progression.Level < requiredLevel)
        {
            return "Requires Club Tier";
        }

        var shortfall = cost - _career.Progression.Credits;
        return shortfall > 0 ? $"Need {shortfall:n0} more credits" : "Ready to upgrade";
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
        RaisePropertyChanged(nameof(CurrentPlayerPortraitLabel));
        RaisePropertyChanged(nameof(ActivePlayerPortraitAssetPath));
        RaisePropertyChanged(nameof(CurrentTitle));
        RaisePropertyChanged(nameof(CurrentLeagueName));
        RaisePropertyChanged(nameof(ProgressText));
        RaisePropertyChanged(nameof(StandingText));
        RaisePropertyChanged(nameof(ActiveCareerSummaryText));
        RaisePropertyChanged(nameof(SeasonSummaryText));
        RaisePropertyChanged(nameof(CareerProgressSummaryText));
        RaisePropertyChanged(nameof(NextEventHeadlineText));
        RaisePropertyChanged(nameof(NextEventDetailsText));
        RaisePropertyChanged(nameof(NextEventDisplayTitle));
        RaisePropertyChanged(nameof(NextEventDisplaySubtitle));
        RaisePropertyChanged(nameof(NextEventRoundDisplay));
        RaisePropertyChanged(nameof(NextEventSessionDisplay));
        RaisePropertyChanged(nameof(NextEventRewardCreditsText));
        RaisePropertyChanged(nameof(NextEventRewardXpText));
        RaisePropertyChanged(nameof(NextEventHeroMetaText));
        RaisePropertyChanged(nameof(NextEventHeroNotesText));
        RaisePropertyChanged(nameof(NextEventOverlapGuidanceText));
        RaisePropertyChanged(nameof(CurrentLeagueDescriptionText));
        RaisePropertyChanged(nameof(CareerProgressSummaryText));
        RaisePropertyChanged(nameof(ChallengeSummaryText));
        RaisePropertyChanged(nameof(GarageEligibleEventText));
        RaisePropertyChanged(nameof(GarageDetailSummaryText));
        RaisePropertyChanged(nameof(SelectedGarageCarTitle));
        RaisePropertyChanged(nameof(SelectedGarageCarClass));
        RaisePropertyChanged(nameof(SelectedGarageCarStatus));
        RaisePropertyChanged(nameof(SelectedGarageCarCondition));
        RaisePropertyChanged(nameof(SelectedGarageCarSummary));
        RaisePropertyChanged(nameof(SelectedGarageCarActionText));
        RaisePropertyChanged(nameof(SelectedGarageCarUnlockText));
        RaisePropertyChanged(nameof(SelectedGarageCarPriceText));
        RaisePropertyChanged(nameof(SelectedGarageCarEligibleEventsText));
        RaisePropertyChanged(nameof(SelectedGarageCarCareerStartsText));
        RaisePropertyChanged(nameof(CanUseSelectedGarageCar));
        RaisePropertyChanged(nameof(CanRepairSelectedGarageCar));
        RaisePropertyChanged(nameof(GarageEmptyStateTitle));
        RaisePropertyChanged(nameof(GarageEmptyStateText));
        RaisePropertyChanged(nameof(HasOwnedCars));
        RaisePropertyChanged(nameof(FeaturedRivalName));
        RaisePropertyChanged(nameof(FeaturedRivalPortraitAssetPath));
        RaisePropertyChanged(nameof(FeaturedRivalStatusText));
        RaisePropertyChanged(nameof(FeaturedRivalHeatText));
        RaisePropertyChanged(nameof(FeaturedRivalMetaText));
        RaisePropertyChanged(nameof(FeaturedRivalHeadToHeadText));
        RaisePropertyChanged(nameof(FeaturedRivalDetailText));
        RaisePropertyChanged(nameof(SelectedRivalName));
        RaisePropertyChanged(nameof(SelectedRivalStatus));
        RaisePropertyChanged(nameof(SelectedRivalHeat));
        RaisePropertyChanged(nameof(SelectedRivalDetail));
        RaisePropertyChanged(nameof(SelectedRivalHeadToHead));
        RaisePropertyChanged(nameof(SelectedRivalRewardPreview));
        RaisePropertyChanged(nameof(SelectedRivalPortraitAssetPath));
        RaisePropertyChanged(nameof(TeamHqSummaryText));
        RaisePropertyChanged(nameof(TeamHqEfficiencyText));
        RaisePropertyChanged(nameof(TeamHqNextUnlockText));
        RaisePropertyChanged(nameof(TeamHqEmptyStateTitle));
        RaisePropertyChanged(nameof(TeamHqEmptyStateText));
        RaisePropertyChanged(nameof(HasTeamHqDepartments));
        RaisePropertyChanged(nameof(SelectedCareerTierTitle));
        RaisePropertyChanged(nameof(SelectedCareerTierStatus));
        RaisePropertyChanged(nameof(SelectedCareerTierDescription));
        RaisePropertyChanged(nameof(SelectedDepartmentName));
        RaisePropertyChanged(nameof(SelectedDepartmentSummary));
        RaisePropertyChanged(nameof(LastResultPrimaryPositionText));
        RaisePropertyChanged(nameof(LastResultMetaText));
        RaisePropertyChanged(nameof(LastResultOutcomeText));
        UpdateProfileSummary();
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
        _dismissPendingResultCommand.RaiseCanExecuteChanged();
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
        RaceDesk.SessionPresetStatusText = SessionPresetStatusText;
        RaceDesk.SelectedSessionPresetSummaryText = SelectedSessionPresetSummaryText;
        RaceDesk.CanDeleteSelectedSessionPreset = CanDeleteSelectedSessionPreset;
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
            RaceDesk.EventPlan.Clear();
            Diagnostics.RecordExportStatus(NextEventExportStatusText);
            UpdateRaceDeskFlowState();
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
        RaceDesk.EventPlan.Update(plan, NextEventDetailsText, NextEventExportStatusText, preview.Readiness);
        Diagnostics.RecordExportStatus(NextEventExportStatusText);
        _automationTraceWriter.AppendExportStatus(NextEventExportStatusText);
        UpdateRaceDeskFlowState();
    }

    private static string BuildExportStatusText(string message, EventExportReadiness? readiness, bool requiresRestart)
    {
        if (readiness is null)
        {
            return message;
        }

        var lines = new List<string> { message };

        if (readiness.BlockingIssues.Count > 0)
        {
            lines.Add($"Needs action: {string.Join(" | ", readiness.BlockingIssues)}");
        }
        else if (readiness.Guidance.Count > 0)
        {
            lines.Add(string.Join(" | ", readiness.Guidance));
        }

        lines.Add(requiresRestart
            ? "Apply it, then restart AMS2 before you launch."
            : "Apply it directly when you are ready.");

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
        RaisePropertyChanged(nameof(TelemetryHealthText));
        UpdateRaceDeskFlowState();
    }

    private void UpdateProfileSummary()
    {
        if (_career is null)
        {
            Profile.Clear();
            return;
        }

        Profile.Update(
            _career.Id,
            _career.Name,
            _career.PlayerProfile.StarterCarName,
            GetCurrentTitle(),
            CurrentLeagueName,
            _career.Progression.Level,
            _career.Progression.Xp,
            _career.Progression.Credits,
            _career.Progression.DriverRating,
            _career.Progression.Reputation);
    }

    private async Task EnsureCareerPortraitsAsync()
    {
        if (_career is null)
        {
            return;
        }

        var previousPlayerPortraitId = _career.PlayerProfile.PortraitId;
        var previousRivalPortraitIds = _career.Rivals.Select(rival => rival.PortraitId).ToArray();

        _driverPortraitService.EnsurePortraitAssignments(_career, _content);

        var changed = !string.Equals(previousPlayerPortraitId, _career.PlayerProfile.PortraitId, StringComparison.OrdinalIgnoreCase)
            || !_career.Rivals.Select(rival => rival.PortraitId).SequenceEqual(previousRivalPortraitIds, StringComparer.OrdinalIgnoreCase);

        if (changed)
        {
            await _repository.SaveCareerAsync(_career, setAsCurrent: true);
        }
    }

    private string ResolvePortraitAssetPath(string? portraitId)
    {
        var portrait = _driverPortraitService.ResolvePortrait(_content, portraitId);
        var relativePath = portrait.AssetPath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        var placeholderPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, DriverPortraitService.PlaceholderAssetPath.Replace('/', Path.DirectorySeparatorChar)));
        return File.Exists(placeholderPath) ? placeholderPath : string.Empty;
    }

    private void UpdateRaceDeskFlowState()
    {
        RaceDesk.ReadinessChecklist.Clear();

        if (_career is null)
        {
            RaceDesk.StateKey = "NoCareer";
            RaceDesk.BlockingGuidance = "Create a career profile before preparing events.";
            RaceDesk.PrimaryAction.Set("Create Career", _createCareerCommand, _createCareerCommand.CanExecute(null), "Primary");
            RaceDesk.SecondaryAction.Clear();
            AddChecklistItem("Career Profile", "Create or load a career profile.", "Missing", "Error");
            return;
        }

        var preview = _nextEventPlan is null ? null : _eventExportAdapter.BuildPreview(_nextEventPlan);
        var automation = _raceAutomationCoordinator.CurrentStatus;
        var hasPendingReview = PendingDraft is not null;
        var telemetryHealthy = _telemetryFeed.ConnectionState == TelemetryConnectionState.Monitoring;

        AddChecklistItem("Career Profile", $"Active profile: {CurrentCareerName}.", "Ready", "Ready");
        AddChecklistItem("Next Event", _nextEventPlan is null ? "Generate the next event plan." : NextEventHeadlineText, _nextEventPlan is null ? "Waiting" : "Ready", _nextEventPlan is null ? "Waiting" : "Ready");
        AddChecklistItem("Event Export", preview?.Success == true ? "Event package is ready to apply." : NextEventExportStatusText, preview?.Success == true ? "Ready" : "Blocked", preview?.Success == true ? "Ready" : "Blocked");
        var sessionStatusText = automation.Stage switch
        {
            RaceAutomationStage.Error => "Error",
            RaceAutomationStage.RaceRunning => "Ready",
            RaceAutomationStage.GridDetected or RaceAutomationStage.WaitingForSession or RaceAutomationStage.LaunchRequested => "Waiting",
            RaceAutomationStage.SessionFinished => "Idle",
            RaceAutomationStage.RestartRequired or RaceAutomationStage.EventPrepared => "Blocked",
            _ => "Idle"
        };

        AddChecklistItem("AMS2 Session", automation.Detail, sessionStatusText, sessionStatusText);
        AddChecklistItem("Telemetry", telemetryHealthy ? "Shared memory feed is healthy." : "Telemetry is offline.", telemetryHealthy ? "Ready" : "Offline", telemetryHealthy ? "Ready" : "Offline");

        if (hasPendingReview)
        {
            RaceDesk.StateKey = "ManualReviewRequired";
            RaceDesk.BlockingGuidance = "A captured result needs manual confirmation before the career can continue.";
            RaceDesk.PrimaryAction.Set("Commit Result", _confirmPendingResultCommand, _confirmPendingResultCommand.CanExecute(null), "Primary");
            RaceDesk.SecondaryAction.Set("Dismiss Result", _dismissPendingResultCommand, _dismissPendingResultCommand.CanExecute(null), "Secondary");
            return;
        }

        switch (automation.Stage)
        {
            case RaceAutomationStage.RaceRunning:
                RaceDesk.StateKey = "RaceRunning";
                RaceDesk.BlockingGuidance = "Race is live. Keep AMS2 running and let monitoring continue.";
                RaceDesk.PrimaryAction.Set("Race Running", null, false, "Neutral");
                RaceDesk.SecondaryAction.Set("Refresh Status", _refreshCommand, true, "Secondary");
                return;
            case RaceAutomationStage.GridDetected:
                RaceDesk.StateKey = "GridDetected";
                RaceDesk.BlockingGuidance = "You are at the grid. Confirm the race screen in AMS2, then start the session.";
                RaceDesk.PrimaryAction.Set("Waiting For Race Start", null, false, "Neutral");
                RaceDesk.SecondaryAction.Set("Refresh Status", _refreshCommand, true, "Secondary");
                return;
            case RaceAutomationStage.WaitingForSession:
                RaceDesk.StateKey = "WaitingForSession";
                RaceDesk.BlockingGuidance = "AMS2 is running. Open the prepared race screen and confirm you are ready.";
                RaceDesk.PrimaryAction.Set("I’m At The Race Screen", _refreshCommand, true, "Primary");
                RaceDesk.SecondaryAction.Clear();
                return;
            case RaceAutomationStage.LaunchRequested:
                RaceDesk.StateKey = "LaunchRequested";
                RaceDesk.BlockingGuidance = "AMS2 launch has been requested. Wait for the session feed to connect.";
                RaceDesk.PrimaryAction.Set("Launching AMS2...", null, false, "Neutral");
                RaceDesk.SecondaryAction.Set("Refresh Status", _refreshCommand, true, "Secondary");
                return;
            case RaceAutomationStage.SessionFinished:
                RaceDesk.StateKey = "ResultReconstructing";
                RaceDesk.BlockingGuidance = "The session has ended. Final result capture is finishing.";
                RaceDesk.PrimaryAction.Set("Continue Career", _refreshCommand, true, "Primary");
                RaceDesk.SecondaryAction.Clear();
                return;
            case RaceAutomationStage.RestartRequired:
                RaceDesk.StateKey = "RestartRequired";
                RaceDesk.BlockingGuidance = "Restart AMS2 so the prepared event can load cleanly.";
                RaceDesk.PrimaryAction.Set("Apply + Restart + Launch AMS2", _applyRecommendedEventAndLaunchCommand, _applyRecommendedEventAndLaunchCommand.CanExecute(null), "Primary");
                RaceDesk.SecondaryAction.Clear();
                return;
            case RaceAutomationStage.EventPrepared:
                RaceDesk.StateKey = "EventPrepared";
                RaceDesk.BlockingGuidance = "The event package is ready to apply.";
                RaceDesk.PrimaryAction.Set("Apply + Restart + Launch AMS2", _applyRecommendedEventAndLaunchCommand, _applyRecommendedEventAndLaunchCommand.CanExecute(null), "Primary");
                RaceDesk.SecondaryAction.Clear();
                return;
            case RaceAutomationStage.Error:
                RaceDesk.StateKey = "Error";
                RaceDesk.BlockingGuidance = automation.Detail;
                RaceDesk.PrimaryAction.Set("Retry", _refreshCommand, true, "Primary");
                RaceDesk.SecondaryAction.Set("Launch AMS2", _launchAms2Command, _launchAms2Command.CanExecute(null), "Secondary");
                return;
        }

        if (_nextEventPlan is null)
        {
            RaceDesk.StateKey = "CareerReady";
            RaceDesk.BlockingGuidance = "Generate the next planned event to continue the career.";
            RaceDesk.PrimaryAction.Set("Prepare Event", _refreshCommand, true, "Primary");
            RaceDesk.SecondaryAction.Clear();
            return;
        }

        if (preview?.Success != true)
        {
            RaceDesk.StateKey = "ExportBlocked";
            RaceDesk.BlockingGuidance = preview?.Message ?? "Event export is currently blocked.";
            RaceDesk.PrimaryAction.Set("Prepare Event", _refreshCommand, true, "Primary");
            RaceDesk.SecondaryAction.Clear();
            return;
        }

        RaceDesk.StateKey = "EventPlanned";
        RaceDesk.BlockingGuidance = preview.RequiresGameRestart
            ? "The event package is ready. Apply it, restart AMS2, and then launch."
            : "The event package is ready. Apply it and launch AMS2.";
        RaceDesk.PrimaryAction.Set(preview.RequiresGameRestart ? "Apply + Restart + Launch AMS2" : "Apply + Launch AMS2", preview.RequiresGameRestart ? _applyRecommendedEventAndLaunchCommand : _applyRecommendedEventCommand, true, "Primary");
        RaceDesk.SecondaryAction.Clear();
    }

    private void RestoreDiagnosticsSnapshot()
    {
        if (!_automationTraceWriter.TryLoadLatestState(out var snapshot))
        {
            return;
        }

        Diagnostics.RecordAutomationStatus(snapshot.CurrentRunId, snapshot.LastAutomationStatus);
        Diagnostics.RecordSessionStatus(snapshot.CurrentRunId, snapshot.LastSessionStatus);
        Diagnostics.RecordExportStatus(snapshot.LastExportStatus);
        Diagnostics.RecordResultStatus(snapshot.CurrentRunId, snapshot.LastResultStatus);
    }

    private void AddChecklistItem(string title, string detail, string statusText, string severity)
    {
        RaceDesk.ReadinessChecklist.Add(new ReadinessChecklistItemViewModel
        {
            Title = title,
            Detail = detail,
            StatusText = statusText,
            Severity = severity
        });
    }

    private string BuildConnectionStateText()
    {
        return _telemetryFeed.ConnectionState switch
        {
            TelemetryConnectionState.Simulating => "AMS2: Test Mode",
            TelemetryConnectionState.Monitoring => "AMS2: Connected",
            _ => _launchService.IsGameRunning ? "AMS2: Running" : "AMS2: Not Running"
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
}
