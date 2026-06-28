using System.Text.Json;

namespace Ams2CareerCompanion.Infrastructure.Launch;

public sealed class Ams2SessionPresetService
{
    private const string ChampionshipEditorFileName = "default.championshipeditor.v1.00.sav";
    private const string CustomChampionshipFileName = "singlechampionshipsave_1_1.sav";
    private readonly string _presetDirectory;
    private readonly string _bundledPresetDirectory;

    public Ams2SessionPresetService(string appDataDirectory)
    {
        _presetDirectory = Path.Combine(appDataDirectory, "session-presets");
        _bundledPresetDirectory = Path.Combine(AppContext.BaseDirectory, "PresetLibrary");
        Directory.CreateDirectory(_presetDirectory);
    }

    public string PresetDirectory => _presetDirectory;
    public string BundledPresetDirectory => _bundledPresetDirectory;
    public string? ProfileDirectory => ResolveProfileDirectory();
    public string ChampionshipEditorFilePath => ChampionshipEditorFileName;
    public string CustomChampionshipFilePath => CustomChampionshipFileName;

    public SessionPresetEnvironmentReport GetEnvironmentReport()
    {
        var profileDirectory = ResolveProfileDirectory();
        if (profileDirectory is null)
        {
            return new SessionPresetEnvironmentReport(
                false,
                null,
                null,
                false,
                "AMS2 profile directory was not found in Documents.",
                ["Launch AMS2 once and create or load a profile before applying prepared events."]);
        }

        var targetFilePath = Path.Combine(profileDirectory, ChampionshipEditorFileName);
        if (!TryVerifyWritableDirectory(profileDirectory, out var writeFailure))
        {
            return new SessionPresetEnvironmentReport(
                false,
                profileDirectory,
                targetFilePath,
                false,
                "AMS2 profile directory is not writable.",
                [writeFailure ?? "Check Documents folder permissions for the AMS2 profile directory."]);
        }

        return new SessionPresetEnvironmentReport(
            true,
            profileDirectory,
            targetFilePath,
            File.Exists(targetFilePath),
            "AMS2 profile target is ready for prepared event export.",
            Array.Empty<string>());
    }

    public SessionPresetEnvironmentReport GetCustomChampionshipEnvironmentReport()
    {
        var profileDirectory = ResolveProfileDirectory();
        if (profileDirectory is null)
        {
            return new SessionPresetEnvironmentReport(
                false,
                null,
                null,
                false,
                "AMS2 profile directory was not found in Documents.",
                ["Launch AMS2 once and create or load a profile before applying prepared events."]);
        }

        var singleChampionshipDirectory = GetSingleChampionshipDirectory(profileDirectory);
        if (!TryVerifyWritableDirectory(singleChampionshipDirectory, out var writeFailure))
        {
            return new SessionPresetEnvironmentReport(
                false,
                singleChampionshipDirectory,
                null,
                false,
                "AMS2 custom championship directory is not writable.",
                [writeFailure ?? "Check Documents folder permissions for the AMS2 custom championship directory."]);
        }

        var targetFilePath = GetCustomChampionshipTargetFilePath(singleChampionshipDirectory);
        return new SessionPresetEnvironmentReport(
            true,
            singleChampionshipDirectory,
            targetFilePath,
            File.Exists(targetFilePath),
            "AMS2 custom championship slot is ready for prepared event export.",
            Array.Empty<string>());
    }

    public IReadOnlyList<SessionPresetInfo> ListPresets()
    {
        var bundled = ListPresetFiles(_bundledPresetDirectory, SessionPresetSource.Bundled);
        var user = ListPresetFiles(_presetDirectory, SessionPresetSource.User);

        return bundled
            .Concat(user)
            .OrderBy(x => x.Source)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public SessionPresetInfo? FindPresetBySlug(string? presetSlug)
    {
        if (string.IsNullOrWhiteSpace(presetSlug))
        {
            return null;
        }

        return ListPresets().FirstOrDefault(x => string.Equals(x.Slug, presetSlug, StringComparison.OrdinalIgnoreCase));
    }

    public PresetOperationResult CaptureCurrentPreset(string presetName)
    {
        var normalizedName = NormalizePresetName(presetName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return new PresetOperationResult(false, "Preset name is required.");
        }

        var sourceFile = GetCurrentChampionshipEditorFile();
        if (sourceFile is null)
        {
            return new PresetOperationResult(false, "AMS2 championship editor save was not found in Documents.");
        }

        var targetFile = GetUserPresetFilePath(normalizedName);
        File.Copy(sourceFile, targetFile, overwrite: true);
        return new PresetOperationResult(true, $"Captured current AMS2 event preset as '{normalizedName}'.", normalizedName);
    }

    public PresetOperationResult ApplyPreset(SessionPresetInfo? preset)
    {
        if (preset is null)
        {
            return new PresetOperationResult(false, "Select a preset first.");
        }

        if (!File.Exists(preset.FilePath))
        {
            return new PresetOperationResult(false, $"Preset '{preset.Name}' was not found.");
        }

        var environment = GetEnvironmentReport();
        if (!environment.IsReady || string.IsNullOrWhiteSpace(environment.ProfileDirectory) || string.IsNullOrWhiteSpace(environment.TargetFilePath))
        {
            return new PresetOperationResult(false, environment.Message);
        }

        return ApplyPresetToTarget(preset, environment.TargetFilePath, "local AMS2 profile");
    }

    public PresetOperationResult ApplyPresetToCustomChampionship(SessionPresetInfo? preset)
    {
        if (preset is null)
        {
            return new PresetOperationResult(false, "Select a preset first.");
        }

        if (!File.Exists(preset.FilePath))
        {
            return new PresetOperationResult(false, $"Preset '{preset.Name}' was not found.");
        }

        var environment = GetCustomChampionshipEnvironmentReport();
        if (!environment.IsReady || string.IsNullOrWhiteSpace(environment.ProfileDirectory) || string.IsNullOrWhiteSpace(environment.TargetFilePath))
        {
            return new PresetOperationResult(false, environment.Message);
        }

        return ApplyPresetToTarget(preset, environment.TargetFilePath, "AMS2 custom championship slot");
    }

    public PresetOperationResult DeletePreset(SessionPresetInfo? preset)
    {
        if (preset is null)
        {
            return new PresetOperationResult(false, "Select a preset first.");
        }

        if (preset.Source != SessionPresetSource.User)
        {
            return new PresetOperationResult(false, "Built-in presets cannot be deleted.");
        }

        if (!File.Exists(preset.FilePath))
        {
            return new PresetOperationResult(false, $"Preset '{preset.Name}' was not found.");
        }

        File.Delete(preset.FilePath);
        var metadataPath = Path.ChangeExtension(preset.FilePath, ".json");
        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }

        return new PresetOperationResult(true, $"Deleted preset '{preset.Name}'.", preset.Name);
    }

    private string? GetCurrentChampionshipEditorFile()
    {
        var profileDirectory = ResolveProfileDirectory();
        if (profileDirectory is null)
        {
            return null;
        }

        var file = Path.Combine(profileDirectory, ChampionshipEditorFileName);
        return File.Exists(file) ? file : null;
    }

    private string GetUserPresetFilePath(string presetName)
    {
        return Path.Combine(_presetDirectory, $"{presetName}.sav");
    }

    private PresetOperationResult ApplyPresetToTarget(SessionPresetInfo preset, string targetFilePath, string targetDescription)
    {
        var targetDirectory = Path.GetDirectoryName(targetFilePath);
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            return new PresetOperationResult(false, $"Could not resolve the AMS2 {targetDescription} path.");
        }

        Directory.CreateDirectory(targetDirectory);
        var backupFile = targetFilePath + ".backup";

        if (File.Exists(targetFilePath))
        {
            File.Copy(targetFilePath, backupFile, overwrite: true);
        }

        File.Copy(preset.FilePath, targetFilePath, overwrite: true);
        var sourceText = preset.Source == SessionPresetSource.Bundled ? "built-in preset" : "saved preset";
        return new PresetOperationResult(true, $"Applied {sourceText} '{preset.Name}' to the {targetDescription}.", preset.Name);
    }

    private static IReadOnlyList<SessionPresetInfo> ListPresetFiles(string rootDirectory, SessionPresetSource source)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return Array.Empty<SessionPresetInfo>();
        }

        return Directory.GetFiles(rootDirectory, "*.sav", SearchOption.TopDirectoryOnly)
            .Select(path => BuildPresetInfo(path, source))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SessionPresetInfo BuildPresetInfo(string presetFilePath, SessionPresetSource source)
    {
        var fileName = Path.GetFileNameWithoutExtension(presetFilePath) ?? "preset";
        var metadataPath = Path.ChangeExtension(presetFilePath, ".json");
        var metadata = LoadMetadata(metadataPath);
        var displayName = string.IsNullOrWhiteSpace(metadata?.Name) ? BuildFriendlyName(fileName) : metadata!.Name!;
        var description = string.IsNullOrWhiteSpace(metadata?.Description)
            ? source == SessionPresetSource.Bundled
                ? "Built-in AMS2 event preset included with the app."
                : "User-captured AMS2 event preset."
            : metadata!.Description!;

        return new SessionPresetInfo(
            Key: $"{source}:{fileName}",
            Name: displayName,
            Slug: fileName,
            Source: source,
            FilePath: presetFilePath,
            Description: description,
            CanDelete: source == SessionPresetSource.User);
    }

    private static SessionPresetMetadata? LoadMetadata(string metadataPath)
    {
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(metadataPath);
            return JsonSerializer.Deserialize<SessionPresetMetadata>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildFriendlyName(string slug)
    {
        return string.Join(' ', slug
            .Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Length == 0
                ? part
                : char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string NormalizePresetName(string? presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            return string.Empty;
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = presetName.Trim().Where(ch => !invalid.Contains(ch)).ToArray();
        return new string(chars);
    }

    private static string? ResolveProfileDirectory()
    {
        var documentsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Automobilista 2", "savegame");
        if (!Directory.Exists(documentsRoot))
        {
            return null;
        }

        var candidates = Directory.GetDirectories(documentsRoot)
            .Select(path => Path.Combine(path, "automobilista 2", "profiles"))
            .Where(Directory.Exists)
            .OrderByDescending(path => Directory.GetLastWriteTimeUtc(path))
            .ToArray();

        return candidates.FirstOrDefault();
    }

    private static string GetSingleChampionshipDirectory(string profileDirectory)
    {
        var profileRoot = Directory.GetParent(profileDirectory)?.FullName;
        if (string.IsNullOrWhiteSpace(profileRoot))
        {
            return profileDirectory;
        }

        return Path.Combine(profileRoot, "singlechamps");
    }

    private static string GetCustomChampionshipTargetFilePath(string singleChampionshipDirectory)
    {
        if (!Directory.Exists(singleChampionshipDirectory))
        {
            Directory.CreateDirectory(singleChampionshipDirectory);
        }

        var existingSlots = Directory.GetFiles(singleChampionshipDirectory, "singlechampionshipsave_*.sav", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ThenByDescending(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return existingSlots.FirstOrDefault()
               ?? Path.Combine(singleChampionshipDirectory, CustomChampionshipFileName);
    }

    private static bool TryVerifyWritableDirectory(string directoryPath, out string? failureReason)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
            var probeFile = Path.Combine(directoryPath, ".ams2-career-write-test");
            File.WriteAllText(probeFile, DateTime.UtcNow.ToString("O"));
            File.Delete(probeFile);
            failureReason = null;
            return true;
        }
        catch (Exception ex)
        {
            failureReason = ex.Message;
            return false;
        }
    }

    private sealed record SessionPresetMetadata(string? Name, string? Description);
}

public enum SessionPresetSource
{
    Bundled = 0,
    User = 1
}

public sealed record SessionPresetInfo(
    string Key,
    string Name,
    string Slug,
    SessionPresetSource Source,
    string FilePath,
    string Description,
    bool CanDelete)
{
    public string DisplayName => Source == SessionPresetSource.Bundled ? $"{Name} (Built-in)" : $"{Name} (User)";
    public string SourceLabel => Source == SessionPresetSource.Bundled ? "Built-in preset" : "User preset";
    public override string ToString() => DisplayName;
}

public sealed record PresetOperationResult(bool Success, string Message, string? PresetName = null);

public sealed record SessionPresetEnvironmentReport(
    bool IsReady,
    string? ProfileDirectory,
    string? TargetFilePath,
    bool TargetFileAlreadyExists,
    string Message,
    IReadOnlyList<string> Guidance);
