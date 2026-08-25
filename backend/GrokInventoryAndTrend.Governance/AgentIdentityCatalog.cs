namespace GrokInventoryAndTrend.Governance;

public static class AgentIdentityCatalog
{
    public const string PolicyBundleVersion = "v1";

    public static string ToMeshAgentId(AgentRole role) =>
        role switch
        {
            AgentRole.SignalIngestion => "did:mesh:inventory-signal-ingestion",
            AgentRole.FeatureAndCausality => "did:mesh:inventory-feature-and-causality",
            AgentRole.Forecasting => "did:mesh:inventory-forecasting",
            AgentRole.ReplenishmentAndAllocation => "did:mesh:inventory-replenishment-and-allocation",
            AgentRole.PlannerCopilot => "did:mesh:inventory-planner-copilot",
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
}
