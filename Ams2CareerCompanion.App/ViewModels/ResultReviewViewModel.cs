using Ams2CareerCompanion.Core.Models;

namespace Ams2CareerCompanion.App.ViewModels;

public sealed class ResultReviewViewModel : ObservableObject
{
    private RaceResultDraft? _pendingDraft;
    private string _summary = "No pending result review.";
    private string _details = "Race result review is idle.";

    public RaceResultDraft? PendingDraft
    {
        get => _pendingDraft;
        private set
        {
            if (SetProperty(ref _pendingDraft, value))
            {
                RaisePropertyChanged(nameof(HasPendingDraft));
            }
        }
    }

    public bool HasPendingDraft => PendingDraft is not null;

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public string Details
    {
        get => _details;
        private set => SetProperty(ref _details, value);
    }

    public void SetPendingDraft(RaceResultDraft draft)
    {
        PendingDraft = draft;
        Summary = draft.Summary;
        Details =
            $"Outcome: {draft.Outcome}\n" +
            $"Confidence: {draft.Confidence}\n" +
            $"Overall: P{draft.OverallPosition}/{draft.Entrants}\n" +
            $"Class: P{draft.ClassPosition}\n" +
            $"Laps: {draft.LapsCompleted}\n" +
            $"Automation run: {(draft.AutomationRunId?.ToString("D") ?? "Not linked")}";
    }

    public void Clear(string status = "No pending result review.")
    {
        PendingDraft = null;
        Summary = status;
        Details = "Race result review is idle.";
    }
}
