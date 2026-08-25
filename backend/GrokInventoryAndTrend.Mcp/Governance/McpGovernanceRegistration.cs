using GrokInventoryAndTrend.Governance;
using GrokInventoryAndTrend.Governance.Audit;
using GrokInventoryAndTrend.Governance.Mcp;
using Microsoft.Extensions.Options;

namespace GrokInventoryAndTrend.Mcp.Governance;

public static class McpGovernanceRegistration
{
    public static IServiceCollection AddMcpGovernance(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GovernanceSettings>(configuration.GetSection(GovernanceSettings.SectionName));
        services.AddHttpContextAccessor();

        services.AddSingleton<IAgentGovernanceAuditStore>(sp =>
        {
            GovernanceSettings options = sp.GetRequiredService<IOptions<GovernanceSettings>>().Value;
            return new FileAgentGovernanceAuditStore(options.AgentAuditStoreDirectory);
        });

        services.AddSingleton<FoundryAgentGovernanceBootstrap>(_ => new FoundryAgentGovernanceBootstrap());

        services.AddSingleton<McpToolGovernanceCoordinator>();

        return services;
    }
}
