using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public sealed class CareerEventPlanner
{
    public CareerEventPlan BuildNextEvent(CareerState state, CareerContentCatalog content, IReadOnlyList<RaceResultConfirmed> history)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(history);

        var league = ResolveLeague(state, content);
        var leagueHistoryCount = history.Count(result => string.Equals(result.Draft.LeagueId, league.Id, StringComparison.Ordinal));
        var eventCount = Math.Max(league.TrackLayoutIds.Count, 1);
        var eventNumber = leagueHistoryCount + 1;

        var layout = ResolveTrackLayout(league, leagueHistoryCount, content);
        var track = content.Tracks.FirstOrDefault(x => string.Equals(x.Id, layout.TrackId, StringComparison.Ordinal));
        var classDefinition = ResolveCarClass(state, league, content);

        return new CareerEventPlan
        {
            LeagueId = league.Id,
            LeagueName = league.Name,
            EventNumber = eventNumber,
            EventCount = eventCount,
            TrackLayoutId = layout.Id,
            TrackDisplayName = string.IsNullOrWhiteSpace(layout.DisplayName) ? league.TrackName : layout.DisplayName,
            TrackId = layout.TrackId,
            TrackName = track?.Name ?? league.TrackName,
            Country = track?.Country ?? string.Empty,
            PlayerCarClassId = classDefinition?.Id ?? league.EligibleCarClassIds.FirstOrDefault() ?? string.Empty,
            PlayerCarClassName = classDefinition?.Name ?? league.ClassName,
            RecommendedGridSize = layout.RecommendedGridSize > 0 ? layout.RecommendedGridSize : league.GridSize,
            RecommendedDriverRating = league.RecommendedDriverRating,
            BaseXpReward = league.BaseXpReward,
            BaseCreditReward = league.BaseCreditReward,
            SuggestedPresetSlug = league.Id,
            SetupNotes = BuildSetupNotes(league, layout, classDefinition)
        };
    }

    private static LeagueDefinition ResolveLeague(CareerState state, CareerContentCatalog content)
    {
        return content.Leagues.FirstOrDefault(x => string.Equals(x.Id, state.ActiveLeagueId, StringComparison.Ordinal))
            ?? content.Leagues.First();
    }

    private static OfficialTrackLayoutDefinition ResolveTrackLayout(LeagueDefinition league, int leagueHistoryCount, CareerContentCatalog content)
    {
        if (league.TrackLayoutIds.Count > 0)
        {
            var nextLayoutId = league.TrackLayoutIds[leagueHistoryCount % league.TrackLayoutIds.Count];
            var knownLayout = content.TrackLayouts.FirstOrDefault(x => string.Equals(x.Id, nextLayoutId, StringComparison.Ordinal));
            if (knownLayout is not null)
            {
                return knownLayout;
            }
        }

        return content.TrackLayouts.FirstOrDefault(x =>
                   string.Equals(x.DisplayName, league.TrackName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(x.Name, league.TrackName, StringComparison.OrdinalIgnoreCase))
               ?? content.TrackLayouts.First();
    }

    private static OfficialCarClassDefinition? ResolveCarClass(CareerState state, LeagueDefinition league, CareerContentCatalog content)
    {
        var playerClassName = state.PlayerProfile.SelectedCarClass;
        var byEligibleId = league.EligibleCarClassIds
            .Select(classId => content.CarClasses.FirstOrDefault(x => string.Equals(x.Id, classId, StringComparison.Ordinal)))
            .FirstOrDefault(x => x is not null);

        if (byEligibleId is not null && string.Equals(byEligibleId.Name, playerClassName, StringComparison.OrdinalIgnoreCase))
        {
            return byEligibleId;
        }

        return content.CarClasses.FirstOrDefault(x => string.Equals(x.Name, playerClassName, StringComparison.OrdinalIgnoreCase))
            ?? byEligibleId;
    }

    private static string BuildSetupNotes(LeagueDefinition league, OfficialTrackLayoutDefinition layout, OfficialCarClassDefinition? classDefinition)
    {
        var lines = new List<string>
        {
            $"League branch: {league.Name}",
            $"Track target: {(string.IsNullOrWhiteSpace(layout.DisplayName) ? league.TrackName : layout.DisplayName)}",
            $"Class target: {classDefinition?.Name ?? league.ClassName}",
            $"Recommended grid: {(layout.RecommendedGridSize > 0 ? layout.RecommendedGridSize : league.GridSize)}"
        };

        if (!string.IsNullOrWhiteSpace(layout.Grade))
        {
            lines.Add($"Venue grade: {layout.Grade}");
        }

        return string.Join(" | ", lines);
    }
}
