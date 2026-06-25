using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ams2CareerCompanion.Infrastructure.Launch;

public sealed class Ams2LaunchService
{
    private const int Ams2SteamAppId = 1066890;
    private static readonly string[] CandidateExecutables = ["AMS2AVX.exe", "AMS2.exe"];
    private static readonly string[] CandidateSteamRoots =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
    ];

    public string? InstallDirectory { get; private set; }
    public string? ExecutablePath { get; private set; }
    public string? SteamExecutablePath { get; private set; }
    public bool IsInstalled => !string.IsNullOrWhiteSpace(ExecutablePath) || !string.IsNullOrWhiteSpace(SteamExecutablePath);
    public bool IsGameRunning => Process.GetProcessesByName("AMS2").Length > 0 || Process.GetProcessesByName("AMS2AVX").Length > 0;

    public void Refresh()
    {
        InstallDirectory = null;
        ExecutablePath = null;
        SteamExecutablePath = null;

        var libraryFoldersPath = FindLibraryFoldersFile();
        if (libraryFoldersPath is null)
        {
            return;
        }

        SteamExecutablePath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(libraryFoldersPath)!)!, "steam.exe");
        foreach (var libraryPath in ReadSteamLibraryPaths(libraryFoldersPath))
        {
            var manifestPath = Path.Combine(libraryPath, "steamapps", $"appmanifest_{Ams2SteamAppId}.acf");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var installDirectory = ResolveInstallDirectory(manifestPath, libraryPath);
            if (installDirectory is null)
            {
                continue;
            }

            InstallDirectory = installDirectory;
            ExecutablePath = CandidateExecutables
                .Select(exe => Path.Combine(installDirectory, exe))
                .FirstOrDefault(File.Exists);

            if (!string.IsNullOrWhiteSpace(ExecutablePath))
            {
                return;
            }
        }
    }

    public LaunchResult Launch()
    {
        Refresh();

        if (IsGameRunning)
        {
            return new LaunchResult(true, false, "AMS2 is already running.");
        }

        if (!string.IsNullOrWhiteSpace(ExecutablePath) && File.Exists(ExecutablePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ExecutablePath,
                WorkingDirectory = Path.GetDirectoryName(ExecutablePath)!,
                UseShellExecute = true
            });

            return new LaunchResult(true, true, $"Launching AMS2 via {Path.GetFileName(ExecutablePath)}.", ExecutablePath);
        }

        if (!string.IsNullOrWhiteSpace(SteamExecutablePath) && File.Exists(SteamExecutablePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{Ams2SteamAppId}",
                UseShellExecute = true
            });

            return new LaunchResult(true, true, "Launching AMS2 via Steam URI.", $"steam://rungameid/{Ams2SteamAppId}");
        }

        return new LaunchResult(false, false, "AMS2 installation was not found. Check Steam library detection in Settings.");
    }

    public LaunchResult LaunchVr()
    {
        Refresh();

        if (IsGameRunning)
        {
            return new LaunchResult(true, false, "AMS2 is already running.");
        }

        if (!string.IsNullOrWhiteSpace(ExecutablePath) && File.Exists(ExecutablePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ExecutablePath,
                Arguments = "-vr",
                WorkingDirectory = Path.GetDirectoryName(ExecutablePath)!,
                UseShellExecute = true
            });

            return new LaunchResult(true, true, "Experimental VR launch requested via AMS2 executable with -vr.", $"{ExecutablePath} -vr");
        }

        if (!string.IsNullOrWhiteSpace(SteamExecutablePath) && File.Exists(SteamExecutablePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{Ams2SteamAppId}",
                UseShellExecute = true
            });

            return new LaunchResult(true, true, "Experimental VR launch requested via Steam. Steam launch preference may decide flat or VR mode.", $"steam://rungameid/{Ams2SteamAppId}");
        }

        return new LaunchResult(false, false, "AMS2 installation was not found. Check Steam library detection in Settings.");
    }

    private static string? FindLibraryFoldersFile()
    {
        foreach (var steamRoot in CandidateSteamRoots.Where(Directory.Exists))
        {
            var libraryFoldersPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFoldersPath))
            {
                return libraryFoldersPath;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ReadSteamLibraryPaths(string libraryFoldersPath)
    {
        var content = File.ReadAllText(libraryFoldersPath);
        var matches = Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\"");
        return matches
            .Select(match => match.Groups[1].Value.Replace("\\\\", "\\"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ResolveInstallDirectory(string manifestPath, string libraryPath)
    {
        var manifestContent = File.ReadAllText(manifestPath);
        var match = Regex.Match(manifestContent, "\"installdir\"\\s+\"([^\"]+)\"");
        if (!match.Success)
        {
            return null;
        }

        var installDirName = match.Groups[1].Value;
        var installDirectory = Path.Combine(libraryPath, "steamapps", "common", installDirName);
        return Directory.Exists(installDirectory) ? installDirectory : null;
    }
}

public sealed record LaunchResult(bool Success, bool LaunchTriggered, string Message, string? LaunchTarget = null);
