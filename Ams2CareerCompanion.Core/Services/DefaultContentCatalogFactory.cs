using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public static class DefaultContentCatalogFactory
{
    public static CareerContentCatalog Create() =>
        new()
        {
            StarterCars =
            [
                new StarterCarDefinition
                {
                    Id = "starter-ginetta",
                    Name = "Ginetta G40 Junior",
                    ClassName = "Rookie Cup",
                    Description = "Balanced, forgiving, and ideal for learning clean racecraft."
                },
                new StarterCarDefinition
                {
                    Id = "starter-copa",
                    Name = "Copa Uno",
                    ClassName = "Rookie Cup",
                    Description = "Momentum-focused hatchback with scrappy club-racing character."
                },
                new StarterCarDefinition
                {
                    Id = "starter-kart",
                    Name = "Rental Kart",
                    ClassName = "Rookie Cup",
                    Description = "High-agility rookie option with lower payouts but faster XP flow."
                }
            ],
            Leagues =
            [
                new LeagueDefinition
                {
                    Id = "rookie-cup",
                    Name = "Rookie Cup",
                    TrackName = "Goiania Short",
                    ClassName = "Rookie Cup",
                    RequiredLevel = 1,
                    RequiredReputation = 0,
                    RecommendedDriverRating = 950,
                    BaseXpReward = 150,
                    BaseCreditReward = 1200
                },
                new LeagueDefinition
                {
                    Id = "club-tour",
                    Name = "Club Tour",
                    TrackName = "Curvelo Club",
                    ClassName = "Club Touring",
                    PrerequisiteLeagueIds = ["rookie-cup"],
                    RequiredLevel = 2,
                    RequiredReputation = 5,
                    RecommendedDriverRating = 1000,
                    BaseXpReward = 225,
                    BaseCreditReward = 2200
                },
                new LeagueDefinition
                {
                    Id = "regional-gt",
                    Name = "Regional GT Challenge",
                    TrackName = "Cascavel",
                    ClassName = "Regional GT",
                    PrerequisiteLeagueIds = ["club-tour"],
                    RequiredLevel = 4,
                    RequiredReputation = 12,
                    RecommendedDriverRating = 1050,
                    BaseXpReward = 325,
                    BaseCreditReward = 3600
                }
            ],
            Titles =
            [
                new CareerTitleDefinition { Id = "title-rookie", Name = "Rookie Initiate", RequiredLevel = 1 },
                new CareerTitleDefinition { Id = "title-club", Name = "Club Charger", RequiredLevel = 3 },
                new CareerTitleDefinition { Id = "title-regional", Name = "Regional Contender", RequiredLevel = 5 }
            ],
            ChallengeTemplates =
            [
                new ChallengeTemplate
                {
                    Id = "daily-finish",
                    Name = "Daily Checkered Flag",
                    Description = "Finish one race today.",
                    Cadence = ChallengeCadence.Daily,
                    Kind = ChallengeKind.FinishRace,
                    Target = 1,
                    BonusXp = 60,
                    BonusCredits = 450
                },
                new ChallengeTemplate
                {
                    Id = "daily-clean",
                    Name = "Daily Clean Hands",
                    Description = "Complete one clean race.",
                    Cadence = ChallengeCadence.Daily,
                    Kind = ChallengeKind.CleanFinish,
                    Target = 1,
                    BonusXp = 90,
                    BonusCredits = 600
                },
                new ChallengeTemplate
                {
                    Id = "weekly-podium",
                    Name = "Weekly Podium Push",
                    Description = "Score two podium finishes this week.",
                    Cadence = ChallengeCadence.Weekly,
                    Kind = ChallengeKind.PodiumFinish,
                    Target = 2,
                    BonusXp = 180,
                    BonusCredits = 1250
                }
            ],
            RivalArchetypes =
            [
                new RivalArchetype { Name = "Marta Alves", Personality = "Calculated", Specialty = "Consistency", BaseRating = 1010 },
                new RivalArchetype { Name = "Luca Moretti", Personality = "Aggressive", Specialty = "Late Braking", BaseRating = 995 },
                new RivalArchetype { Name = "Tiago Rocha", Personality = "Opportunist", Specialty = "Wet Weather", BaseRating = 1005 }
            ]
        };
}
