namespace GrokInventoryAndTrend.Governance;

public static class AgentCatalog
{
    public const string SignalIngestionAgentName = "signal-ingestion-agent";
    public const string FeatureAndCausalityAgentName = "feature-and-causality-agent";
    public const string ForecastingAgentName = "forecasting-agent";
    public const string ReplenishmentAndAllocationAgentName = "replenishment-and-allocation-agent";
    public const string PlannerCopilotAgentName = "planner-copilot-agent";

    public static string ToFolderName(AgentRole role) =>
        role switch
        {
            AgentRole.SignalIngestion => SignalIngestionAgentName,
            AgentRole.FeatureAndCausality => FeatureAndCausalityAgentName,
            AgentRole.Forecasting => ForecastingAgentName,
            AgentRole.ReplenishmentAndAllocation => ReplenishmentAndAllocationAgentName,
            AgentRole.PlannerCopilot => PlannerCopilotAgentName,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };

    public static AgentRole FromFolderName(string folderName)
    {
        if (string.Equals(folderName, SignalIngestionAgentName, StringComparison.OrdinalIgnoreCase))
        {
            return AgentRole.SignalIngestion;
        }

        if (string.Equals(folderName, FeatureAndCausalityAgentName, StringComparison.OrdinalIgnoreCase))
        {
            return AgentRole.FeatureAndCausality;
        }

        if (string.Equals(folderName, ForecastingAgentName, StringComparison.OrdinalIgnoreCase))
        {
            return AgentRole.Forecasting;
        }

        if (string.Equals(folderName, ReplenishmentAndAllocationAgentName, StringComparison.OrdinalIgnoreCase))
        {
            return AgentRole.ReplenishmentAndAllocation;
        }

        if (string.Equals(folderName, PlannerCopilotAgentName, StringComparison.OrdinalIgnoreCase))
        {
            return AgentRole.PlannerCopilot;
        }

        throw new ArgumentException($"Unknown agent folder name '{folderName}'.", nameof(folderName));
    }

    public static IReadOnlyList<AgentRole> AllRoles { get; } =
    [
        AgentRole.SignalIngestion,
        AgentRole.FeatureAndCausality,
        AgentRole.Forecasting,
        AgentRole.ReplenishmentAndAllocation,
        AgentRole.PlannerCopilot
    ];

    public static bool TryResolveRoleFromMcpPath(string? path, out AgentRole role)
    {
        role = default;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.Contains("/signal-ingestion/", StringComparison.OrdinalIgnoreCase))
        {
            role = AgentRole.SignalIngestion;
            return true;
        }

        if (path.Contains("/feature-and-causality/", StringComparison.OrdinalIgnoreCase))
        {
            role = AgentRole.FeatureAndCausality;
            return true;
        }

        if (path.Contains("/forecasting/", StringComparison.OrdinalIgnoreCase))
        {
            role = AgentRole.Forecasting;
            return true;
        }

        if (path.Contains("/replenishment-and-allocation/", StringComparison.OrdinalIgnoreCase))
        {
            role = AgentRole.ReplenishmentAndAllocation;
            return true;
        }

        if (path.Contains("/planner-copilot/", StringComparison.OrdinalIgnoreCase))
        {
            role = AgentRole.PlannerCopilot;
            return true;
        }

        return false;
    }
}
