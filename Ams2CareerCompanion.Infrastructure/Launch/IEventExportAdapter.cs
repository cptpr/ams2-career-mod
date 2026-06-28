using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.Infrastructure.Launch;

public interface IEventExportAdapter
{
    string AdapterId { get; }

    EventExportPreview BuildPreview(CareerEventPlan? eventPlan);

    EventExportResult Apply(CareerEventPlan? eventPlan);
}
