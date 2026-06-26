using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Core.Interfaces;

public interface ICareerRepository
{
    Task<CareerState?> LoadCurrentCareerAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CareerSummary>> ListCareersAsync(CancellationToken cancellationToken = default);
    Task SaveCareerAsync(CareerState career, bool setAsCurrent, CancellationToken cancellationToken = default);
    Task SetCurrentCareerAsync(Guid careerId, CancellationToken cancellationToken = default);
    Task DeleteCareerAsync(Guid careerId, CancellationToken cancellationToken = default);
    Task<bool> HasLoggedRaceAsync(Guid careerId, Guid draftId, Guid? automationRunId, CancellationToken cancellationToken = default);
    Task AppendRaceResultAsync(Guid careerId, RaceResultConfirmed result, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RaceResultConfirmed>> LoadRecentResultsAsync(Guid careerId, int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RaceResultConfirmed>> LoadRaceHistoryAsync(Guid careerId, CancellationToken cancellationToken = default);
}
