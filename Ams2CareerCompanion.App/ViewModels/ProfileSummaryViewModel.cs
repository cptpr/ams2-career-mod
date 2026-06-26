namespace Ams2CareerCompanion.App.ViewModels;

public sealed class ProfileSummaryViewModel : ObservableObject
{
    private Guid? _careerId;
    private string _name = "No active career";
    private string _starterCar = "Choose a starter car";
    private string _title = "No title";
    private string _activeLeagueName = "No active league";
    private int _level;
    private int _xp;
    private int _credits;
    private double _driverRating;
    private int _reputation;

    public Guid? CareerId
    {
        get => _careerId;
        private set => SetProperty(ref _careerId, value);
    }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public string StarterCar
    {
        get => _starterCar;
        private set => SetProperty(ref _starterCar, value);
    }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string ActiveLeagueName
    {
        get => _activeLeagueName;
        private set => SetProperty(ref _activeLeagueName, value);
    }

    public int Level
    {
        get => _level;
        private set => SetProperty(ref _level, value);
    }

    public int Xp
    {
        get => _xp;
        private set => SetProperty(ref _xp, value);
    }

    public int Credits
    {
        get => _credits;
        private set => SetProperty(ref _credits, value);
    }

    public double DriverRating
    {
        get => _driverRating;
        private set => SetProperty(ref _driverRating, value);
    }

    public int Reputation
    {
        get => _reputation;
        private set => SetProperty(ref _reputation, value);
    }

    public void Clear()
    {
        CareerId = null;
        Name = "No active career";
        StarterCar = "Choose a starter car";
        Title = "No title";
        ActiveLeagueName = "No active league";
        Level = 0;
        Xp = 0;
        Credits = 0;
        DriverRating = 0;
        Reputation = 0;
    }

    public void Update(Guid careerId, string name, string starterCar, string title, string activeLeagueName, int level, int xp, int credits, double driverRating, int reputation)
    {
        CareerId = careerId;
        Name = name;
        StarterCar = starterCar;
        Title = title;
        ActiveLeagueName = activeLeagueName;
        Level = level;
        Xp = xp;
        Credits = credits;
        DriverRating = driverRating;
        Reputation = reputation;
    }
}
