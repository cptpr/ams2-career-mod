using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public sealed class CareerProgressionEngine
{
    public CareerUpdateResult ApplyRaceResult(CareerState state, RaceResultConfirmed result, CareerContentCatalog content)
    {
        var league = ResolveLeague(state, result.Draft, content);
        var reward = BuildBaseReward(result.Draft, league);

        state.Progression.Xp += reward.XpDelta;
        state.Progression.Credits += reward.CreditsDelta;
        state.Progression.DriverRating = Math.Max(800, state.Progression.DriverRating + reward.DriverRatingDelta);
        state.Progression.Reputation = Math.Max(0, state.Progression.Reputation + reward.ReputationDelta);
        state.Progression.Level = CalculateLevel(state.Progression.Xp);

        UpdateChallenges(state, result.Draft, reward);
        UpdateTitles(state, content, reward);

        if (result.Draft.OverallPosition <= league.RequiredCompletionPosition &&
            !state.CompletedLeagueIds.Contains(league.Id, StringComparer.Ordinal))
        {
            state.CompletedLeagueIds.Add(league.Id);
            reward.LeagueCompleted = true;
            reward.Unlocks.Add($"League completed: {league.Name}");
        }

        UnlockLeagues(state, content, reward);
        var featuredRival = UpdateRivals(state, result.Draft, league);

        return new CareerUpdateResult
        {
            RewardBreakdown = reward,
            FeaturedRival = featuredRival
        };
    }

    public static int CalculateLevel(int xp) => Math.Max(1, xp / 300 + 1);

    private static LeagueDefinition ResolveLeague(CareerState state, RaceResultDraft draft, CareerContentCatalog content)
    {
        if (!string.IsNullOrWhiteSpace(draft.LeagueId))
        {
            var byId = content.Leagues.FirstOrDefault(x => x.Id == draft.LeagueId);
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(state.ActiveLeagueId))
        {
            var byActiveId = content.Leagues.FirstOrDefault(x => x.Id == state.ActiveLeagueId);
            if (byActiveId is not null)
            {
                return byActiveId;
            }
        }

        if (!string.IsNullOrWhiteSpace(draft.LeagueName))
        {
            var byName = content.Leagues.FirstOrDefault(x => string.Equals(x.Name, draft.LeagueName, StringComparison.OrdinalIgnoreCase));
            if (byName is not null)
            {
                return byName;
            }
        }

        return content.Leagues.First();
    }

    private static RewardBreakdown BuildBaseReward(RaceResultDraft draft, LeagueDefinition league)
    {
        if (draft.Outcome is not RaceOutcome.Finished)
        {
            var ratingPenalty = draft.Outcome == RaceOutcome.Disqualified ? -18 : -10;
            var reputationPenalty = draft.Outcome == RaceOutcome.Disqualified ? -2 : 0;

            return new RewardBreakdown
            {
                XpDelta = Math.Max(15, league.BaseXpReward / 6),
                CreditsDelta = Math.Max(0, league.BaseCreditReward / 10),
                DriverRatingDelta = ratingPenalty,
                ReputationDelta = reputationPenalty
            };
        }

        var placementFactor = Math.Max(0, draft.Entrants - draft.OverallPosition);
        var cleanBonusXp = draft.IsCleanRace ? 40 : 0;
        var cleanBonusCredits = draft.IsCleanRace ? 300 : 0;

        return new RewardBreakdown
        {
            XpDelta = league.BaseXpReward + placementFactor * 25 + cleanBonusXp,
            CreditsDelta = league.BaseCreditReward + placementFactor * 175 + cleanBonusCredits,
            DriverRatingDelta = Math.Round((draft.Entrants / 2.0 - draft.OverallPosition) * 4, 1),
            ReputationDelta = draft.OverallPosition <= 3 ? 4 : (draft.OverallPosition <= draft.Entrants / 2 ? 2 : 0)
        };
    }

    private static void UpdateChallenges(CareerState state, RaceResultDraft draft, RewardBreakdown reward)
    {
        foreach (var challenge in state.Challenges.Where(x => !x.IsCompleted))
        {
            switch (challenge.Kind)
            {
                case ChallengeKind.FinishRace:
                    if (draft.Outcome != RaceOutcome.Finished)
                    {
                        break;
                    }
                    challenge.Progress = Math.Min(challenge.Target, challenge.Progress + 1);
                    break;
                case ChallengeKind.CleanFinish:
                    if (draft.Outcome == RaceOutcome.Finished && draft.IsCleanRace)
                    {
                        challenge.Progress = Math.Min(challenge.Target, challenge.Progress + 1);
                    }
                    break;
                case ChallengeKind.PodiumFinish:
                    if (draft.Outcome == RaceOutcome.Finished && draft.OverallPosition <= 3)
                    {
                        challenge.Progress = Math.Min(challenge.Target, challenge.Progress + 1);
                    }
                    break;
            }

            if (!challenge.IsCompleted && challenge.Progress >= challenge.Target)
            {
                challenge.IsCompleted = true;
                reward.XpDelta += challenge.BonusXp;
                reward.CreditsDelta += challenge.BonusCredits;
                reward.CompletedChallenges.Add(challenge.Name);
                reward.Unlocks.Add($"Challenge complete: {challenge.Name}");
            }
        }
    }

    private static void UpdateTitles(CareerState state, CareerContentCatalog content, RewardBreakdown reward)
    {
        foreach (var title in content.Titles.Where(x => x.RequiredLevel <= state.Progression.Level))
        {
            if (state.UnlockedTitleIds.Contains(title.Id, StringComparer.Ordinal))
            {
                continue;
            }

            state.UnlockedTitleIds.Add(title.Id);
            reward.Unlocks.Add($"Title unlocked: {title.Name}");
        }
    }

    private static void UnlockLeagues(CareerState state, CareerContentCatalog content, RewardBreakdown reward)
    {
        foreach (var league in content.Leagues)
        {
            if (state.UnlockedLeagueIds.Contains(league.Id, StringComparer.Ordinal))
            {
                continue;
            }

            var prerequisitesMet = league.PrerequisiteLeagueIds.All(id => state.CompletedLeagueIds.Contains(id, StringComparer.Ordinal));
            var thresholdsMet = state.Progression.Level >= league.RequiredLevel && state.Progression.Reputation >= league.RequiredReputation;

            if (!prerequisitesMet || !thresholdsMet)
            {
                continue;
            }

            state.UnlockedLeagueIds.Add(league.Id);
            reward.Unlocks.Add($"League unlocked: {league.Name}");

            if (string.Equals(state.ActiveLeagueId, league.Id, StringComparison.Ordinal))
            {
                continue;
            }

            if (state.CompletedLeagueIds.Contains(state.ActiveLeagueId, StringComparer.Ordinal))
            {
                state.ActiveLeagueId = league.Id;
                reward.NewlyActiveLeagueName = league.Name;
            }
        }
    }

    private static RivalProfile? UpdateRivals(CareerState state, RaceResultDraft draft, LeagueDefinition league)
    {
        var featured = state.Rivals
            .OrderByDescending(x => x.RivalryIntensity)
            .ThenBy(x => Math.Abs(x.DriverRating - state.Progression.DriverRating))
            .FirstOrDefault();

        if (featured is null)
        {
            return null;
        }

        featured.DriverRating = Math.Max(850, featured.DriverRating + Random.Shared.NextDouble() * 12 - 4);
        featured.Reputation = Math.Max(0, featured.Reputation + (draft.Outcome == RaceOutcome.Finished && draft.OverallPosition <= 3 ? 2 : 1));

        var encounter = BuildEncounterRecord(state, draft, featured, league);
        featured.RecentEncounters.Insert(0, encounter);
        if (featured.RecentEncounters.Count > 5)
        {
            featured.RecentEncounters.RemoveRange(5, featured.RecentEncounters.Count - 5);
        }

        if (draft.Outcome == RaceOutcome.Finished && draft.OverallPosition <= 3)
        {
            featured.RivalryIntensity = Math.Min(100, featured.RivalryIntensity + 10);
        }
        else
        {
            featured.RivalryIntensity = Math.Min(100, featured.RivalryIntensity + 4);
        }

        if (draft.Outcome == RaceOutcome.Finished && draft.OverallPosition == 1)
        {
            featured.RivalryIntensity = Math.Min(100, featured.RivalryIntensity + 6);
        }

        return featured;
    }

    private static RivalEncounterRecord BuildEncounterRecord(CareerState state, RaceResultDraft draft, RivalProfile rival, LeagueDefinition league)
    {
        var spread = GetDeterministicSpread(state.CareerSeed, rival.Id, draft.Id, 1, Math.Max(2, Math.Min(4, league.GridSize / 3)));
        var playerFinishedAhead = draft.Outcome == RaceOutcome.Finished && (draft.OverallPosition <= 3 || draft.IsCleanRace);
        var rivalPosition = playerFinishedAhead
            ? Math.Min(league.GridSize, draft.OverallPosition + spread)
            : Math.Max(1, draft.OverallPosition - spread);
        var rivalryDelta = playerFinishedAhead
            ? Math.Clamp(8 - spread, 3, 10)
            : Math.Clamp(5 - spread, 1, 4);

        return new RivalEncounterRecord
        {
            CompletedUtc = draft.CompletedUtc,
            LeagueId = league.Id,
            LeagueName = league.Name,
            TrackName = draft.TrackName,
            PlayerPosition = draft.OverallPosition,
            RivalPosition = rivalPosition,
            PlayerFinishedAhead = playerFinishedAhead,
            RivalryDelta = rivalryDelta,
            Summary = $"{league.Name} at {draft.TrackName}: P{draft.OverallPosition} vs P{rivalPosition}."
        };
    }

    private static int GetDeterministicSpread(int careerSeed, string rivalId, Guid draftId, int minInclusive, int maxInclusive)
    {
        if (maxInclusive <= minInclusive)
        {
            return minInclusive;
        }

        var hash = HashCode.Combine(careerSeed, rivalId, draftId);
        var value = hash == int.MinValue ? 0 : Math.Abs(hash);
        return minInclusive + (value % (maxInclusive - minInclusive + 1));
    }
}
