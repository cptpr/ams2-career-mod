using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public static class DefaultContentCatalogFactory
{
    public static CareerContentCatalog Create() =>
        new()
        {
            CarClasses =
            [
                new OfficialCarClassDefinition
                {
                    Id = "rookie-cup",
                    Name = "Rookie Cup",
                    Family = "Entry",
                    Tier = "Rookie",
                    IsStarterEligible = true,
                    Description = "Accessible rookie machinery for the first career branch."
                },
                new OfficialCarClassDefinition
                {
                    Id = "club-touring",
                    Name = "Club Touring",
                    Family = "Tin Top",
                    Tier = "Club",
                    Description = "Early club-racing touring and hatchback content."
                },
                new OfficialCarClassDefinition
                {
                    Id = "regional-gt",
                    Name = "Regional GT",
                    Family = "GT",
                    Tier = "Regional",
                    Description = "Faster regional GT content with stronger fields."
                }
            ],
            Cars =
            [
                new OfficialCarDefinition
                {
                    Id = "ginetta-g40-junior",
                    ClassId = "rookie-cup",
                    Manufacturer = "Ginetta",
                    Name = "G40 Junior"
                },
                new OfficialCarDefinition
                {
                    Id = "fiat-copa-uno",
                    ClassId = "rookie-cup",
                    Manufacturer = "Fiat",
                    Name = "Uno"
                },
                new OfficialCarDefinition
                {
                    Id = "rental-kart",
                    ClassId = "rookie-cup",
                    Manufacturer = "Kart",
                    Name = "Rental Kart"
                }
            ],
            Tracks =
            [
                new OfficialTrackDefinition { Id = "goiania", Name = "Goiania", Country = "Brazil" },
                new OfficialTrackDefinition { Id = "curvelo", Name = "Curvelo", Country = "Brazil" },
                new OfficialTrackDefinition { Id = "cascavel", Name = "Cascavel", Country = "Brazil" }
            ],
            TrackLayouts =
            [
                new OfficialTrackLayoutDefinition
                {
                    Id = "goiania-short",
                    TrackId = "goiania",
                    Name = "Short",
                    DisplayName = "Goiania Short",
                    RecommendedGridSize = 12,
                    Grade = "Club",
                    SupportsKarts = true
                },
                new OfficialTrackLayoutDefinition
                {
                    Id = "curvelo-club",
                    TrackId = "curvelo",
                    Name = "Club",
                    DisplayName = "Curvelo Club",
                    RecommendedGridSize = 16,
                    Grade = "Club"
                },
                new OfficialTrackLayoutDefinition
                {
                    Id = "cascavel-international",
                    TrackId = "cascavel",
                    Name = "International",
                    DisplayName = "Cascavel",
                    RecommendedGridSize = 20,
                    Grade = "GT"
                }
            ],
            StarterCars =
            [
                new StarterCarDefinition
                {
                    Id = "starter-ginetta",
                    CarId = "ginetta-g40-junior",
                    Name = "Ginetta G40 Junior",
                    ClassName = "Rookie Cup",
                    Description = "Balanced, forgiving, and ideal for learning clean racecraft."
                },
                new StarterCarDefinition
                {
                    Id = "starter-copa",
                    CarId = "fiat-copa-uno",
                    Name = "Copa Uno",
                    ClassName = "Rookie Cup",
                    Description = "Momentum-focused hatchback with scrappy club-racing character."
                },
                new StarterCarDefinition
                {
                    Id = "starter-kart",
                    CarId = "rental-kart",
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
                    TrackLayoutIds = ["goiania-short"],
                    EligibleCarClassIds = ["rookie-cup"],
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
                    TrackLayoutIds = ["curvelo-club"],
                    EligibleCarClassIds = ["club-touring"],
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
                    TrackLayoutIds = ["cascavel-international"],
                    EligibleCarClassIds = ["regional-gt"],
                    PrerequisiteLeagueIds = ["club-tour"],
                    RequiredLevel = 4,
                    RequiredReputation = 12,
                    RecommendedDriverRating = 1050,
                    BaseXpReward = 325,
                    BaseCreditReward = 3600
                }
            ],
            EventTemplates =
            [
                new EventTemplateDefinition
                {
                    Id = "rookie-cup-goiania",
                    LeagueId = "rookie-cup",
                    Name = "Rookie Sprint at Goiania",
                    TrackLayoutId = "goiania-short",
                    EligibleCarClassIds = ["rookie-cup"],
                    PresetSlug = "bundled-rookie-event",
                    SetupInstructions = "Use the bundled rookie preset and confirm the selected rookie tin-top in AMS2."
                },
                new EventTemplateDefinition
                {
                    Id = "club-tour-curvelo",
                    LeagueId = "club-tour",
                    Name = "Club Tour at Curvelo",
                    TrackLayoutId = "curvelo-club",
                    EligibleCarClassIds = ["club-touring"],
                    PresetSlug = "bundled-rookie-event",
                    SetupInstructions = "Temporary preset placeholder until the club touring preset library is added."
                },
                new EventTemplateDefinition
                {
                    Id = "regional-gt-cascavel",
                    LeagueId = "regional-gt",
                    Name = "Regional GT at Cascavel",
                    TrackLayoutId = "cascavel-international",
                    EligibleCarClassIds = ["regional-gt"],
                    PresetSlug = "bundled-rookie-event",
                    SetupInstructions = "Temporary preset placeholder until the regional GT preset library is added."
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
            DriverPortraits =
            [
                DriverPortraitService.CreatePlaceholderDefinition()
            ],
            RivalArchetypes =
            [
                new RivalArchetype { Name = "Marta Alves", Personality = "Calculated", Specialty = "Consistency", BaseRating = 1010 },
                new RivalArchetype { Name = "Luca Moretti", Personality = "Aggressive", Specialty = "Late Braking", BaseRating = 995 },
                new RivalArchetype { Name = "Tiago Rocha", Personality = "Opportunist", Specialty = "Wet Weather", BaseRating = 1005 }
            ]
        };
}
