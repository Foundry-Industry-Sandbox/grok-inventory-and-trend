using System.Text.Json;
using GrokInventoryAndTrend.Governance;
using Microsoft.Extensions.Options;

namespace GrokInventoryAndTrend.Mcp.Governance;

public sealed class McpAgentRoleMiddleware
{
    private readonly RequestDelegate _next;

    public McpAgentRoleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        GovernanceSettings settings = context.RequestServices
            .GetRequiredService<IOptions<GovernanceSettings>>()
            .Value;

        if (!settings.EnableMcpToolGovernance || !IsMcpRequest(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!AgentCatalog.TryResolveRoleFromMcpPath(context.Request.Path.Value, out _))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { error = "Unable to resolve agent role from MCP route." }),
                    context.RequestAborted)
                .ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool IsMcpRequest(PathString path)
    {
        string value = path.Value ?? string.Empty;
        return value.Contains("/signal-ingestion/", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/feature-and-causality/", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/forecasting/", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/replenishment-and-allocation/", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/planner-copilot/", StringComparison.OrdinalIgnoreCase);
    }
}
