using System.Text.Json;
using System.Text.Json.Serialization;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Infrastructure.OfficialContent;

public sealed class FileCareerContentCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _baseDirectory;
    private readonly string _contentFilePath;

    public FileCareerContentCatalogLoader(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
        _contentFilePath = Path.Combine(baseDirectory, "content", "official", "official-content.json");
    }

    public string ContentFilePath => _contentFilePath;

    public CareerContentCatalog Load()
    {
        if (!File.Exists(_contentFilePath))
        {
            throw new FileNotFoundException("Official content file was not found.", _contentFilePath);
        }

        var json = File.ReadAllText(_contentFilePath);
        var catalog = JsonSerializer.Deserialize<CareerContentCatalog>(json, JsonOptions)
            ?? throw new InvalidOperationException("Official content file did not deserialize into a career catalog.");

        Validate(catalog, _baseDirectory);
        return catalog;
    }

    private static void Validate(CareerContentCatalog catalog, string baseDirectory)
    {
        EnsureCount(catalog.StarterCars, nameof(catalog.StarterCars), 3);
        EnsureCount(catalog.Leagues, nameof(catalog.Leagues), 1);
        EnsureCount(catalog.Titles, nameof(catalog.Titles), 1);
        EnsureCount(catalog.ChallengeTemplates, nameof(catalog.ChallengeTemplates), 1);
        EnsureCount(catalog.DriverPortraits, nameof(catalog.DriverPortraits), 34);
        EnsureCount(catalog.RivalArchetypes, nameof(catalog.RivalArchetypes), 1);
        EnsureCount(catalog.EventTemplates, nameof(catalog.EventTemplates), 1);
        EnsureCount(catalog.CarClasses, nameof(catalog.CarClasses), 1);
        EnsureCount(catalog.Cars, nameof(catalog.Cars), 1);
        EnsureCount(catalog.Tracks, nameof(catalog.Tracks), 1);
        EnsureCount(catalog.TrackLayouts, nameof(catalog.TrackLayouts), 1);

        EnsureUniqueIds(catalog.CarClasses.Select(x => x.Id), "car class");
        EnsureUniqueIds(catalog.Cars.Select(x => x.Id), "car");
        EnsureUniqueIds(catalog.Tracks.Select(x => x.Id), "track");
        EnsureUniqueIds(catalog.TrackLayouts.Select(x => x.Id), "track layout");
        EnsureUniqueIds(catalog.StarterCars.Select(x => x.Id), "starter car");
        EnsureUniqueIds(catalog.Leagues.Select(x => x.Id), "league");
        EnsureUniqueIds(catalog.EventTemplates.Select(x => x.Id), "event template");
        EnsureUniqueIds(catalog.Titles.Select(x => x.Id), "title");
        EnsureUniqueIds(catalog.ChallengeTemplates.Select(x => x.Id), "challenge");
        EnsureUniqueIds(catalog.DriverPortraits.Select(x => x.Id), "driver portrait");

        var carClassIds = catalog.CarClasses.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var carIds = catalog.Cars.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var trackIds = catalog.Tracks.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var layoutIds = catalog.TrackLayouts.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var leagueIds = catalog.Leagues.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var car in catalog.Cars)
        {
            EnsureReference(car.ClassId, carClassIds, $"car '{car.Id}' class");
        }

        foreach (var layout in catalog.TrackLayouts)
        {
            EnsureReference(layout.TrackId, trackIds, $"track layout '{layout.Id}' track");
        }

        foreach (var starterCar in catalog.StarterCars)
        {
            EnsureReference(starterCar.CarId, carIds, $"starter car '{starterCar.Id}' car");
        }

        foreach (var league in catalog.Leagues)
        {
            foreach (var prerequisiteId in league.PrerequisiteLeagueIds)
            {
                EnsureReference(prerequisiteId, leagueIds, $"league '{league.Id}' prerequisite");
            }

            foreach (var trackLayoutId in league.TrackLayoutIds)
            {
                EnsureReference(trackLayoutId, layoutIds, $"league '{league.Id}' track layout");
            }

            foreach (var classId in league.EligibleCarClassIds)
            {
                EnsureReference(classId, carClassIds, $"league '{league.Id}' eligible car class");
            }
        }

        foreach (var template in catalog.EventTemplates)
        {
            EnsureReference(template.LeagueId, leagueIds, $"event template '{template.Id}' league");
            EnsureReference(template.TrackLayoutId, layoutIds, $"event template '{template.Id}' track layout");

            foreach (var classId in template.EligibleCarClassIds)
            {
                EnsureReference(classId, carClassIds, $"event template '{template.Id}' eligible car class");
            }
        }

        foreach (var portrait in catalog.DriverPortraits)
        {
            EnsureValue(portrait.AssetPath, $"driver portrait '{portrait.Id}' asset path");
            EnsureValue(portrait.DisplayLabel, $"driver portrait '{portrait.Id}' display label");
            EnsureAssetExists(baseDirectory, portrait.AssetPath, $"driver portrait '{portrait.Id}' asset");
        }
    }

    private static void EnsureCount<T>(IReadOnlyCollection<T> values, string label, int minimum)
    {
        if (values.Count < minimum)
        {
            throw new InvalidOperationException($"Official content is missing required '{label}' entries. Expected at least {minimum}.");
        }
    }

    private static void EnsureUniqueIds(IEnumerable<string> ids, string label)
    {
        var duplicate = ids
            .GroupBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1);

        if (duplicate is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(duplicate.Key))
        {
            throw new InvalidOperationException($"Official content contains a {label} entry with an empty id.");
        }

        throw new InvalidOperationException($"Official content contains duplicate {label} id '{duplicate.Key}'.");
    }

    private static void EnsureReference(string id, IReadOnlySet<string> knownIds, string label)
    {
        if (!knownIds.Contains(id))
        {
            throw new InvalidOperationException($"Official content reference '{label}' points to missing id '{id}'.");
        }
    }

    private static void EnsureValue(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Official content value '{label}' is required.");
        }
    }

    private static void EnsureAssetExists(string baseDirectory, string relativePath, string label)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, normalizedPath));
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Official content asset '{label}' was not found at '{fullPath}'.");
        }
    }
}
