using System.Security.Cryptography;
using System.Text;
using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Services;

public sealed class DriverPortraitService
{
    public const string PlaceholderPortraitId = "portrait-placeholder";
    public const string PlaceholderAssetPath = "content/official/driver-portraits/placeholder.png";
    public const string PlaceholderLabel = "Portrait Placeholder";

    public IReadOnlyList<DriverPortraitDefinition> GetPlayerPortraitOptions(CareerContentCatalog content)
    {
        return GetPortraits(content, DriverPortraitAvailability.Player, includePlaceholder: false);
    }

    public IReadOnlyList<DriverPortraitDefinition> GetRivalPortraitOptions(CareerContentCatalog content)
    {
        return GetPortraits(content, DriverPortraitAvailability.Rival, includePlaceholder: false);
    }

    public void EnsurePortraitAssignments(CareerState state, CareerContentCatalog content, string? requestedPlayerPortraitId = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(content);

        state.CareerSeed = state.CareerSeed == 0 ? DeriveCareerSeed(state.Id) : state.CareerSeed;

        var playerPortraits = GetPlayerPortraitOptions(content);
        var rivalPortraits = GetRivalPortraitOptions(content);

        if (string.IsNullOrWhiteSpace(state.PlayerProfile.PortraitId) || !IsAvailable(content, state.PlayerProfile.PortraitId, DriverPortraitAvailability.Player))
        {
            state.PlayerProfile.PortraitId = ChoosePlayerPortrait(playerPortraits, requestedPlayerPortraitId, state.CareerSeed)?.Id ?? PlaceholderPortraitId;
        }

        if (state.Rivals.Count == 0)
        {
            return;
        }

        var rankedRivalPortraits = RankPortraits(rivalPortraits, state.CareerSeed, "rival");
        if (rankedRivalPortraits.Count == 0)
        {
            foreach (var rival in state.Rivals)
            {
                rival.PortraitId = PlaceholderPortraitId;
            }

            return;
        }

        var reservedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(state.PlayerProfile.PortraitId))
        {
            reservedIds.Add(state.PlayerProfile.PortraitId);
        }

        foreach (var rival in state.Rivals.Where(rival => !string.IsNullOrWhiteSpace(rival.PortraitId) && IsAvailable(content, rival.PortraitId, DriverPortraitAvailability.Rival)))
        {
            reservedIds.Add(rival.PortraitId);
        }

        var openPool = rankedRivalPortraits.Where(portrait => !reservedIds.Contains(portrait.Id)).ToList();
        var fallbackPool = openPool.Count > 0 ? openPool : rankedRivalPortraits.ToList();
        var assignmentCursor = 0;

        foreach (var rival in state.Rivals.OrderBy(rival => rival.Name, StringComparer.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(rival.PortraitId) && IsAvailable(content, rival.PortraitId, DriverPortraitAvailability.Rival))
            {
                continue;
            }

            if (assignmentCursor >= fallbackPool.Count)
            {
                assignmentCursor = 0;
            }

            rival.PortraitId = fallbackPool[assignmentCursor].Id;
            assignmentCursor++;
        }
    }

    public DriverPortraitDefinition ResolvePortrait(CareerContentCatalog content, string? portraitId)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!string.IsNullOrWhiteSpace(portraitId))
        {
            var existing = content.DriverPortraits.FirstOrDefault(portrait => string.Equals(portrait.Id, portraitId, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                return existing;
            }
        }

        return content.DriverPortraits.FirstOrDefault(portrait => string.Equals(portrait.Id, PlaceholderPortraitId, StringComparison.OrdinalIgnoreCase))
            ?? CreatePlaceholderDefinition();
    }

    public static DriverPortraitDefinition CreatePlaceholderDefinition()
    {
        return new DriverPortraitDefinition
        {
            Id = PlaceholderPortraitId,
            AssetPath = PlaceholderAssetPath,
            DisplayLabel = PlaceholderLabel,
            Tags = ["placeholder"],
            Availability = DriverPortraitAvailability.Both
        };
    }

    private static IReadOnlyList<DriverPortraitDefinition> RankPortraits(IEnumerable<DriverPortraitDefinition> portraits, int seed, string scope)
    {
        return portraits
            .OrderBy(portrait => ComputeStableSortKey(seed, scope, portrait.Id))
            .ThenBy(portrait => portrait.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static DriverPortraitDefinition? ChoosePlayerPortrait(
        IReadOnlyList<DriverPortraitDefinition> playerPortraits,
        string? requestedPlayerPortraitId,
        int seed)
    {
        if (!string.IsNullOrWhiteSpace(requestedPlayerPortraitId))
        {
            var requested = playerPortraits.FirstOrDefault(portrait => string.Equals(portrait.Id, requestedPlayerPortraitId, StringComparison.OrdinalIgnoreCase));
            if (requested is not null)
            {
                return requested;
            }
        }

        return RankPortraits(playerPortraits, seed, "player").FirstOrDefault();
    }

    private static IReadOnlyList<DriverPortraitDefinition> GetPortraits(
        CareerContentCatalog content,
        DriverPortraitAvailability availability,
        bool includePlaceholder)
    {
        return content.DriverPortraits
            .Where(portrait => includePlaceholder || !portrait.Tags.Contains("placeholder", StringComparer.OrdinalIgnoreCase))
            .Where(portrait => availability switch
            {
                DriverPortraitAvailability.Player => portrait.Availability is DriverPortraitAvailability.Player or DriverPortraitAvailability.Both,
                DriverPortraitAvailability.Rival => portrait.Availability is DriverPortraitAvailability.Rival or DriverPortraitAvailability.Both,
                _ => true
            })
            .ToArray();
    }

    private static bool IsAvailable(CareerContentCatalog content, string portraitId, DriverPortraitAvailability availability)
    {
        return GetPortraits(content, availability, includePlaceholder: true)
            .Any(portrait => string.Equals(portrait.Id, portraitId, StringComparison.OrdinalIgnoreCase));
    }

    private static int DeriveCareerSeed(Guid id)
    {
        return BitConverter.ToInt32(id.ToByteArray(), 0);
    }

    private static string ComputeStableSortKey(int seed, string scope, string portraitId)
    {
        var input = $"{seed}:{scope}:{portraitId}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
