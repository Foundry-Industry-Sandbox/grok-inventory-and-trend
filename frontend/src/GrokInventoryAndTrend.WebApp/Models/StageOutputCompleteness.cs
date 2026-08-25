using GrokInventoryAndTrend.WebApp.Contracts;

namespace GrokInventoryAndTrend.WebApp.Models;

public static class StageOutputCompleteness
{
    public const int StablePollsRequired = 2;

    public static bool IsComplete(
        WorkflowStageKey stageKey,
        AgentStageResult? output,
        string? rawOutput,
        int stablePollCount)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            return false;
        }

        if (MeetsSchema(stageKey, output))
        {
            return true;
        }

        return stablePollCount >= StablePollsRequired;
    }

    public static bool MeetsSchema(WorkflowStageKey stageKey, AgentStageResult? output)
    {
        if (!HasBaseFields(output))
        {
            return false;
        }

        return stageKey switch
        {
            WorkflowStageKey.SignalIngestion =>
                output is SignalIngestionStageResult signal && signal.SourcesIngested.Count > 0,
            WorkflowStageKey.FeatureAndCausality =>
                output is FeatureCausalityStageResult feature && feature.TopDrivers.Count > 0,
            WorkflowStageKey.Forecasting =>
                output is ForecastingStageResult forecast
                && !string.IsNullOrWhiteSpace(forecast.ConfidenceLevel),
            WorkflowStageKey.ReplenishmentAndAllocation =>
                output is ReplenishmentStageResult replenishment && replenishment.LineItems.Count > 0,
            WorkflowStageKey.PlannerCopilot =>
                output is PlannerCopilotStageResult planner
                && !string.IsNullOrWhiteSpace(planner.ApprovalAssessment),
            _ => true
        };
    }

    private static bool HasBaseFields(AgentStageResult? output) =>
        output is not null
        && !string.IsNullOrWhiteSpace(output.Summary)
        && !string.IsNullOrWhiteSpace(output.Decision);
}
