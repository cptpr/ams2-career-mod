using System.IO;
using System.Windows;
using Ams2CareerCompanion.App.ViewModels;
using Ams2CareerCompanion.Core.Models;
using Ams2CareerCompanion.Core.Services;
using Ams2CareerCompanion.Infrastructure.Diagnostics;
using Ams2CareerCompanion.Infrastructure.Launch;
using Ams2CareerCompanion.Infrastructure.OfficialContent;
using Ams2CareerCompanion.Infrastructure.Persistence;
using Ams2CareerCompanion.Infrastructure.Telemetry;

namespace Ams2CareerCompanion.App;

public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            TryWriteFatalLog(args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                TryWriteFatalLog(ex);
            }
        };

        try
        {
            base.OnStartup(e);

            var contentLoader = new FileCareerContentCatalogLoader(AppContext.BaseDirectory);
            var content = TryLoadContentCatalog(contentLoader);
            var dataDirectory = ResolveWritableDataDirectory();

            var repository = new SqliteCareerRepository(dataDirectory);
            var liveTelemetryFeed = new Ams2SharedMemoryFeed(dataDirectory);
            var mockTelemetryFeed = new MockAms2TelemetryFeed();
            var telemetryFeed = new CompositeTelemetryFeed(liveTelemetryFeed, mockTelemetryFeed);
            var launchService = new Ams2LaunchService();
            var sessionPresetService = new Ams2SessionPresetService(dataDirectory);
            var eventExportAdapter = new ChampionshipEditorPresetExportAdapter(sessionPresetService);
            var runContext = new RaceAutomationRunContext();
            var resultService = new ResultReconstructionService(telemetryFeed, runContext);
            var automationTraceWriter = new RaceAutomationTraceWriter(dataDirectory);
            var raceAutomationCoordinator = new RaceAutomationCoordinator(telemetryFeed, launchService, eventExportAdapter, runContext, automationTraceWriter);
            var careerFactory = new CareerFactory();
            var progressionEngine = new CareerProgressionEngine();

            var viewModel = new MainViewModel(
                content,
                repository,
                telemetryFeed,
                telemetryFeed,
                launchService,
                sessionPresetService,
                raceAutomationCoordinator,
                resultService,
                careerFactory,
                progressionEngine);

            var window = new MainWindow
            {
                DataContext = viewModel
            };

            MainWindow = window;
            window.Show();
            await viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            TryWriteFatalLog(ex);
            MessageBox.Show(
                $"Startup failed.\n\n{ex.GetType().Name}: {ex.Message}\n\nSee startup-error.log next to the app for details.",
                "AMS2 Career Companion",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private static string ResolveWritableDataDirectory()
    {
        var candidateRoots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ams2CareerCompanion"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ams2CareerCompanion"),
            Path.Combine(AppContext.BaseDirectory, "data"),
            Path.Combine(Path.GetTempPath(), "Ams2CareerCompanion")
        };

        Exception? lastException = null;

        foreach (var candidate in candidateRoots.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            try
            {
                Directory.CreateDirectory(candidate);
                var probeFile = Path.Combine(candidate, ".write-test");
                File.WriteAllText(probeFile, DateTime.UtcNow.ToString("O"));
                File.Delete(probeFile);
                return candidate;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw new InvalidOperationException("No writable application data directory could be created.", lastException);
    }

    private static void TryWriteFatalLog(Exception ex)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
            File.WriteAllText(path, $"{DateTime.UtcNow:O}{Environment.NewLine}{ex}");
        }
        catch
        {
            // Ignore secondary logging failures.
        }
    }

    private static CareerContentCatalog TryLoadContentCatalog(FileCareerContentCatalogLoader contentLoader)
    {
        try
        {
            return contentLoader.Load();
        }
        catch (Exception ex)
        {
            TryWriteFatalLog(new InvalidOperationException(
                $"Official content catalog failed to load from '{contentLoader.ContentFilePath}'. Falling back to built-in defaults.",
                ex));
            return DefaultContentCatalogFactory.Create();
        }
    }
}
