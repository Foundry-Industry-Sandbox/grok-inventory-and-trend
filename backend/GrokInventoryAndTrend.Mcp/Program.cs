using System.Collections.Concurrent;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using GrokInventoryAndTrend.Mcp;
using GrokInventoryAndTrend.Mcp.Adapters;
using GrokInventoryAndTrend.Mcp.Governance;
using GrokInventoryAndTrend.Mcp.Options;
using GrokInventoryAndTrend.Mcp.Startup;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

if (args.Contains("--bootstrap-foundry-iq", StringComparer.OrdinalIgnoreCase))
{
    var bootstrapBuilder = WebApplication.CreateBuilder(args);
    bootstrapBuilder.Logging.ClearProviders();
    bootstrapBuilder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    });
    bootstrapBuilder.Logging.SetMinimumLevel(LogLevel.Information);
    ConfigureAzureMonitorTelemetry(bootstrapBuilder);
    bootstrapBuilder.Configuration.AddJsonFile("appsettings.Deployment.local.json", optional: true, reloadOnChange: true);
    bootstrapBuilder.Configuration.AddJsonFile("appsettings.Bootstrap.local.json", optional: true, reloadOnChange: true);
    bootstrapBuilder.Configuration.AddEnvironmentVariables();
    bootstrapBuilder.Services.AddInventoryPlanningMcpServices(bootstrapBuilder.Configuration);
    bootstrapBuilder.Services.Configure<FoundryIqBootstrapOptions>(
        bootstrapBuilder.Configuration.GetSection(FoundryIqBootstrapOptions.SectionName));
    bootstrapBuilder.Services.AddSingleton<FoundryIqBootstrapRunner>();

    var bootstrapApp = bootstrapBuilder.Build();
    var exitCode = await bootstrapApp.Services
        .GetRequiredService<FoundryIqBootstrapRunner>()
        .RunAsync(CancellationToken.None);

    await bootstrapApp.DisposeAsync();
    Environment.ExitCode = exitCode;
    return;
}

var builder = WebApplication.CreateBuilder(args);

ConfigureAzureMonitorTelemetry(builder);

builder.Configuration.AddJsonFile("appsettings.Deployment.local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Fabric.local.json", optional: true, reloadOnChange: true);

builder.Services.AddInventoryPlanningMcpServices(builder.Configuration);
builder.Services.AddMcpGovernance(builder.Configuration);

var toolDictionary = new ConcurrentDictionary<string, McpServerTool[]>(StringComparer.OrdinalIgnoreCase);

builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
        options.ConfigureSessionOptions = (httpContext, mcpOptions, _) =>
        {
            var path = httpContext.Request.Path.Value ?? string.Empty;
            var serverKey = ResolveServerKey(path);

            if (!toolDictionary.TryGetValue(serverKey, out var tools))
            {
                return Task.CompletedTask;
            }

            mcpOptions.ToolCollection = [];
            foreach (var tool in tools)
            {
                mcpOptions.ToolCollection.Add(tool);
            }

            return Task.CompletedTask;
        };
    });

var app = builder.Build();

var dataSourceOptions = app.Services.GetRequiredService<IOptions<DataSourceOptions>>().Value;
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("GrokInventoryAndTrend.Mcp.Startup");
if (dataSourceOptions.Mode == DataSourceMode.Fabric)
{
    startupLogger.LogInformation(
        "Planning data source: Fabric (workspace={WorkspaceName}, lakehouse={LakehouseName}, evidenceRoot={EvidenceRoot})",
        dataSourceOptions.FabricLakehouse?.WorkspaceName,
        dataSourceOptions.FabricLakehouse?.LakehouseName,
        string.IsNullOrWhiteSpace(dataSourceOptions.FabricLakehouse?.EvidenceRoot)
            ? "Files/bronze"
            : dataSourceOptions.FabricLakehouse.EvidenceRoot);
}
else
{
    startupLogger.LogInformation("Planning data source: Local");
}

ServiceCollectionExtensions.PopulateToolDictionary(app.Services, toolDictionary);

app.UseMiddleware<McpAgentRoleMiddleware>();

app.MapMcp("/signal-ingestion/mcp");
app.MapMcp("/feature-and-causality/mcp");
app.MapMcp("/forecasting/mcp");
app.MapMcp("/replenishment-and-allocation/mcp");
app.MapMcp("/planner-copilot/mcp");
app.MapGet("/health", async (IServiceProvider services, CancellationToken cancellationToken) =>
{
    var dsOptions = services.GetRequiredService<IOptions<DataSourceOptions>>().Value;
    if (dsOptions.Mode != DataSourceMode.Fabric)
    {
        return Results.Ok(new { status = "ok", dataSource = "Local" });
    }

    try
    {
        var client = services.GetRequiredService<IFabricLakehouseClient>();
        var evidenceRoot = string.IsNullOrWhiteSpace(dsOptions.FabricLakehouse?.EvidenceRoot)
            ? "Files/bronze"
            : dsOptions.FabricLakehouse.EvidenceRoot;
        _ = await client.ListFilesAsync(evidenceRoot, recursive: true, cancellationToken);
        return Results.Ok(new { status = "ok", dataSource = "Fabric", fabricReachable = true });
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("GrokInventoryAndTrend.Mcp.Health");
        logger.LogWarning(ex, "Fabric OneLake health probe failed.");
        return Results.Json(
            new { status = "degraded", dataSource = "Fabric", fabricReachable = false },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

static string ResolveServerKey(string path)
{
    if (path.Contains("/signal-ingestion/", StringComparison.OrdinalIgnoreCase))
    {
        return "signal-ingestion";
    }

    if (path.Contains("/feature-and-causality/", StringComparison.OrdinalIgnoreCase))
    {
        return "feature-and-causality";
    }

    if (path.Contains("/forecasting/", StringComparison.OrdinalIgnoreCase))
    {
        return "forecasting";
    }

    if (path.Contains("/replenishment-and-allocation/", StringComparison.OrdinalIgnoreCase))
    {
        return "replenishment-and-allocation";
    }

    if (path.Contains("/planner-copilot/", StringComparison.OrdinalIgnoreCase))
    {
        return "planner-copilot";
    }

    return "signal-ingestion";
}

static void ConfigureAzureMonitorTelemetry(WebApplicationBuilder builder)
{
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
    {
        builder.Services.AddOpenTelemetry().UseAzureMonitor();
    }
}
