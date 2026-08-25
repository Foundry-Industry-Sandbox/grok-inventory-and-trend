namespace GrokInventoryAndTrend.Mcp.Adapters;

public interface IFabricLakehouseClient
{
    Task<bool> FileExistsAsync(string relativePath, CancellationToken cancellationToken = default);

    Task<string> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListFilesAsync(
        string relativeDirectory,
        bool recursive = true,
        CancellationToken cancellationToken = default);
}
