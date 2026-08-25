using GrokInventoryAndTrend.WebApp.Contracts;
using GrokInventoryAndTrend.WebApp.Models;

namespace GrokInventoryAndTrend.WebApp.State;

/// <summary>
/// Presents workflow stages sequentially: the next card does not advance to
/// Running until the current stage is complete (schema or stability heuristic).
/// Status badges are gated; stage content always reflects the latest backend poll.
/// </summary>
internal sealed class WorkflowStageSequencer
{
    private string? _executionId;
    private readonly Dictionary<WorkflowStageKey, FinalizedStage> _finalized = new();
    private readonly Dictionary<WorkflowStageKey, StabilityState> _stability = new();

    private sealed record FinalizedStage(
        AgentStageResult Output,
        string RawOutput,
        DateTimeOffset? CompletedAt);

    private sealed record StabilityState(string LastRawOutput, int ConsecutiveStablePolls);

    public void ResetForExecution(string? executionId)
    {
        if (string.Equals(_executionId, executionId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _finalized.Clear();
        _stability.Clear();
        _executionId = executionId;
    }

    public void Clear()
    {
        _finalized.Clear();
        _stability.Clear();
        _executionId = null;
    }

    public WorkflowProgressResponse Apply(string executionId, WorkflowProgressResponse progress)
    {
        ResetForExecution(executionId);

        if (progress.Status is not WorkflowRunStatus.Running)
        {
            return progress;
        }

        var sourceStages = progress.Stages;
        if (sourceStages.Count == 0)
        {
            return progress;
        }

        var activeIndex = FindActiveIndex(sourceStages);
        var mergedStages = new List<WorkflowStageProgress>(sourceStages.Count);

        for (var index = 0; index < sourceStages.Count; index++)
        {
            var stage = sourceStages[index];

            if (_finalized.TryGetValue(stage.StageKey, out var finalized))
            {
                mergedStages.Add(ToCompletedStage(stage, finalized));
                continue;
            }

            if (index == activeIndex)
            {
                mergedStages.Add(BuildActiveStage(stage));
                continue;
            }

            mergedStages.Add(ToPendingStage(stage));
        }

        return new WorkflowProgressResponse
        {
            PlanId = progress.PlanId,
            ExecutionId = progress.ExecutionId,
            Status = progress.Status,
            CurrentStage = ResolveCurrentStage(mergedStages, activeIndex),
            StatusMessage = progress.StatusMessage,
            Stages = mergedStages,
            HumanDecision = progress.HumanDecision
        };
    }

    private int FindActiveIndex(IReadOnlyList<WorkflowStageProgress> stages)
    {
        for (var index = 0; index < stages.Count; index++)
        {
            if (!_finalized.ContainsKey(stages[index].StageKey))
            {
                return index;
            }
        }

        return stages.Count - 1;
    }

    private WorkflowStageProgress BuildActiveStage(WorkflowStageProgress stage)
    {
        var rawOutput = stage.RawOutput;
        if (string.IsNullOrWhiteSpace(rawOutput) || stage.Output is null)
        {
            return new WorkflowStageProgress
            {
                StageKey = stage.StageKey,
                Title = stage.Title,
                Status = "Running",
                CompletedAt = null,
                Output = null,
                RawOutput = null
            };
        }

        var stablePollCount = TrackStability(stage.StageKey, rawOutput);
        if (StageOutputCompleteness.IsComplete(stage.StageKey, stage.Output, rawOutput, stablePollCount))
        {
            var finalized = new FinalizedStage(stage.Output, rawOutput, stage.CompletedAt);
            _finalized[stage.StageKey] = finalized;
            _stability.Remove(stage.StageKey);
            return ToCompletedStage(stage, finalized);
        }

        return new WorkflowStageProgress
        {
            StageKey = stage.StageKey,
            Title = stage.Title,
            Status = "Running",
            CompletedAt = null,
            Output = stage.Output,
            RawOutput = rawOutput
        };
    }

    private int TrackStability(WorkflowStageKey stageKey, string rawOutput)
    {
        if (_stability.TryGetValue(stageKey, out var state)
            && string.Equals(state.LastRawOutput, rawOutput, StringComparison.Ordinal))
        {
            var updated = state with { ConsecutiveStablePolls = state.ConsecutiveStablePolls + 1 };
            _stability[stageKey] = updated;
            return updated.ConsecutiveStablePolls;
        }

        _stability[stageKey] = new StabilityState(rawOutput, 1);
        return 1;
    }

    private static WorkflowStageProgress ToCompletedStage(
        WorkflowStageProgress stage,
        FinalizedStage finalized) =>
        new()
        {
            StageKey = stage.StageKey,
            Title = stage.Title,
            Status = "Completed",
            CompletedAt = finalized.CompletedAt ?? stage.CompletedAt,
            Output = stage.Output ?? finalized.Output,
            RawOutput = !string.IsNullOrWhiteSpace(stage.RawOutput)
                ? stage.RawOutput
                : finalized.RawOutput
        };

    private static WorkflowStageProgress ToPendingStage(WorkflowStageProgress stage) =>
        new()
        {
            StageKey = stage.StageKey,
            Title = stage.Title,
            Status = "Pending",
            CompletedAt = null,
            Output = stage.Output,
            RawOutput = stage.RawOutput
        };

    private static WorkflowStageKey? ResolveCurrentStage(
        IReadOnlyList<WorkflowStageProgress> stages,
        int activeIndex)
    {
        if (activeIndex >= 0 && activeIndex < stages.Count)
        {
            var active = stages[activeIndex];
            if (active.Status is "Running" or "Pending")
            {
                return active.StageKey;
            }
        }

        return stages.FirstOrDefault(s => s.Status == "Running")?.StageKey
               ?? stages.FirstOrDefault(s => s.Status == "Pending")?.StageKey
               ?? WorkflowStageKey.PlannerCopilot;
    }
}
