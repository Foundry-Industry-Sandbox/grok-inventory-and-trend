using System.Collections.Concurrent;
using System.Reflection;
using GrokInventoryAndTrend.Governance;
using GrokInventoryAndTrend.Governance.Mcp;
using GrokInventoryAndTrend.Mcp.Adapters;
using GrokInventoryAndTrend.Mcp.Builders;
using GrokInventoryAndTrend.Mcp.Governance;
using GrokInventoryAndTrend.Mcp.Options;
using GrokInventoryAndTrend.Mcp.Startup;
using GrokInventoryAndTrend.Mcp.Tools;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace GrokInventoryAndTrend.Mcp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryPlanningMcpServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatasetOptions>(configuration.GetSection(DatasetOptions.SectionName));
        services.Configure<FoundryIqOptions>(configuration.GetSection(FoundryIqOptions.SectionName));
        services.Configure<DataSourceOptions>(configuration.GetSection(DataSourceOptions.SectionName));

        RegisterPlanningDataStore(services, configuration);

        var foundryIqOptions = configuration.GetSection(FoundryIqOptions.SectionName).Get<FoundryIqOptions>()
            ?? new FoundryIqOptions();

        if (string.IsNullOrWhiteSpace(foundryIqOptions.SearchEndpoint))
        {
            throw new InvalidOperationException("FoundryIq:SearchEndpoint is required.");
        }

        services.AddSingleton<FoundryIqRetrievalService>();
        services.AddSingleton<PlanningDataAdapter>();
        services.AddSingleton<KnowledgeEntryParser>();
        services.AddSingleton<PolicyParser>();
        services.AddSingleton<PolicyIndexAdapter>();
        services.AddSingleton<SignalEvidenceSearcher>();
        services.AddSingleton<LocalKnowledgeAdapter>();
        services.AddSingleton<ReplenishmentPlanBuilder>();

        services.AddSingleton<SignalIngestionTools>();
        services.AddSingleton<FeatureAndCausalityTools>();
        services.AddSingleton<ForecastingTools>();
        services.AddSingleton<ReplenishmentAndAllocationTools>();
        services.AddSingleton<PlannerCopilotTools>();

        return services;
    }

    public static void PopulateToolDictionary(
        IServiceProvider serviceProvider,
        ConcurrentDictionary<string, McpServerTool[]> toolDictionary)
    {
        GovernanceSettings settings = serviceProvider
            .GetRequiredService<IOptions<GovernanceSettings>>()
            .Value;
        McpToolGovernanceCoordinator? coordinator = settings.EnableMcpToolGovernance
            ? serviceProvider.GetRequiredService<McpToolGovernanceCoordinator>()
            : null;
        IHttpContextAccessor? httpContextAccessor = settings.EnableMcpToolGovernance
            ? serviceProvider.GetRequiredService<IHttpContextAccessor>()
            : null;

        toolDictionary["signal-ingestion"] = CreateTools(
            serviceProvider.GetRequiredService<SignalIngestionTools>(),
            coordinator,
            httpContextAccessor);
        toolDictionary["feature-and-causality"] = CreateTools(
            serviceProvider.GetRequiredService<FeatureAndCausalityTools>(),
            coordinator,
            httpContextAccessor);
        toolDictionary["forecasting"] = CreateTools(
            serviceProvider.GetRequiredService<ForecastingTools>(),
            coordinator,
            httpContextAccessor);
        toolDictionary["replenishment-and-allocation"] = CreateTools(
            serviceProvider.GetRequiredService<ReplenishmentAndAllocationTools>(),
            coordinator,
            httpContextAccessor);
        toolDictionary["planner-copilot"] = CreateTools(
            serviceProvider.GetRequiredService<PlannerCopilotTools>(),
            coordinator,
            httpContextAccessor);
    }

    private static McpServerTool[] CreateTools<T>(
        T target,
        McpToolGovernanceCoordinator? coordinator,
        IHttpContextAccessor? httpContextAccessor)
    {
        var tools = new List<McpServerTool>();
        var methods = typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null);

        foreach (var method in methods)
        {
            McpServerTool innerTool = McpServerTool.Create(method, target);
            tools.Add(coordinator is not null && httpContextAccessor is not null
                ? new GovernedMcpServerTool(innerTool, coordinator, httpContextAccessor)
                : innerTool);
        }

        return tools.ToArray();
    }

    private static void RegisterPlanningDataStore(IServiceCollection services, IConfiguration configuration)
    {
        var dsOptions = configuration.GetSection(DataSourceOptions.SectionName).Get<DataSourceOptions>()
            ?? new DataSourceOptions();

        if (dsOptions.Mode == DataSourceMode.Fabric)
        {
            if (string.IsNullOrWhiteSpace(dsOptions.FabricLakehouse?.WorkspaceName)
                || string.IsNullOrWhiteSpace(dsOptions.FabricLakehouse?.LakehouseName))
            {
                throw new InvalidOperationException(
                    "DataSource:Mode is Fabric but FabricLakehouse:WorkspaceName and FabricLakehouse:LakehouseName are required.");
            }

            services.AddSingleton<IFabricLakehouseClient>(sp => FabricLakehouseClient.Create(
                dsOptions,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<FabricLakehouseClient>()));
            services.AddSingleton<IPlanningDataStore, FabricPlanningDataStore>();
            return;
        }

        services.AddSingleton<IPlanningDataStore, LocalPlanningDataStore>();
    }
}
