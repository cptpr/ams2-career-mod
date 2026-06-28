using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public sealed class CareerFactory
{
    private readonly DriverPortraitService _driverPortraitService = new();

    public CareerState CreateNewCareer(
        string careerName,
        StarterCarDefinition starterCar,
        CareerPreset preset,
        CareerContentCatalog content,
        string? playerPortraitId = null,
        int? careerSeed = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(careerName);

        var starterCarClassId = content.Cars
            .FirstOrDefault(x => string.Equals(x.Id, starterCar.CarId, StringComparison.Ordinal))
            ?.ClassId;

        var rookieLeague = content.Leagues.FirstOrDefault(league =>
                league.RequiredLevel <= 1 &&
                (
                    (!string.IsNullOrWhiteSpace(starterCarClassId) && league.EligibleCarClassIds.Contains(starterCarClassId, StringComparer.Ordinal)) ||
                    string.Equals(league.ClassName, starterCar.ClassName, StringComparison.OrdinalIgnoreCase)
                ))
            ?? content.Leagues.First();

        var state = new CareerState
        {
            Name = careerName.Trim(),
            CreatedUtc = DateTime.UtcNow,
            CareerSeed = careerSeed ?? Random.Shared.Next(1, int.MaxValue),
            Preset = preset,
            ActiveLeagueId = rookieLeague.Id,
            PlayerProfile = new PlayerProfile
            {
                StarterCarId = starterCar.Id,
                StarterCarName = starterCar.Name,
                StarterCarClass = starterCar.ClassName,
                SelectedCarClass = starterCar.ClassName,
                PortraitId = playerPortraitId ?? string.Empty
            },
            Progression = new ProgressionState
            {
                Credits = preset switch
                {
                    CareerPreset.Casual => 5000,
                    CareerPreset.Standard => 3000,
                    CareerPreset.Hardcore => 1800,
                    _ => 3000
                },
                DriverRating = 1000,
                Reputation = 0,
                Xp = 0,
                Level = 1
            },
            UnlockedLeagueIds = [rookieLeague.Id],
            UnlockedTitleIds = [content.Titles.First().Id]
        };

        state.Challenges.AddRange(content.ChallengeTemplates.Select(template => new ChallengeInstance
        {
            TemplateId = template.Id,
            Name = template.Name,
            Description = template.Description,
            Cadence = template.Cadence,
            Kind = template.Kind,
            Target = template.Target,
            BonusXp = template.BonusXp,
            BonusCredits = template.BonusCredits
        }));

        foreach (var archetype in content.RivalArchetypes)
        {
            state.Rivals.Add(new RivalProfile
            {
                Name = archetype.Name,
                Personality = archetype.Personality,
                Specialty = archetype.Specialty,
                CurrentLeagueId = rookieLeague.Id,
                DriverRating = archetype.BaseRating,
                Reputation = 0,
                RivalryIntensity = 12,
                PortraitId = string.Empty,
                RecentEncounters = new List<RivalEncounterRecord>()
            });
        }

        _driverPortraitService.EnsurePortraitAssignments(state, content, playerPortraitId);

        return state;
    }
}
