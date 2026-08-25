using Azure;
using GrokInventoryAndTrend.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GrokInventoryAndTrend.Mcp.Adapters;

public sealed class FabricPlanningDataStore : IPlanningDataStore
{
    private readonly IFabricLakehouseClient _client;
    private readonly DatasetOptions _datasetOptions;
    private readonly string _evidenceRoot;
    private readonly ILogger<FabricPlanningDataStore> _logger;

    public FabricPlanningDataStore(
        IFabricLakehouseClient client,
        IOptions<DataSourceOptions> dataSourceOptions,
        IOptions<DatasetOptions> datasetOptions,
        ILogger<FabricPlanningDataStore> logger)
    {
        _client = client;
        _datasetOptions = datasetOptions.Value;
        _logger = logger;
        _evidenceRoot = string.IsNullOrWhiteSpace(dataSourceOptions.Value.FabricLakehouse?.EvidenceRoot)
            ? "Files/bronze"
            : dataSourceOptions.Value.FabricLakehouse!.EvidenceRoot;
    }

    public async Task<string> ReadDocumentAsync(string caseId, SignalCategory category, string fileName, CancellationToken cancellationToken = default)
    {
        ValidateCaseId(caseId);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name must be provided.", nameof(fileName));
        }

        var path = FilePath(caseId, category, fileName);
        try
        {
            return await _client.ReadFileAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException ex)
        {
            throw new FileNotFoundException($"Planning signal document not found: {path}", ex);
        }
    }

    public async Task<IReadOnlyList<string>> ListDocumentsAsync(string caseId, SignalCategory category, CancellationToken cancellationToken = default)
    {
        ValidateCaseId(caseId);
        var categoryPath = CategoryPath(caseId, category);

        IReadOnlyList<string> all;
        try
        {
            all = await _client.ListFilesAsync(categoryPath, recursive: false, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("Category path {CategoryPath} not found (404).", categoryPath);
            return [];
        }

        _logger.LogDebug("DFS listing at {CategoryPath} returned {Count} entries.", categoryPath, all.Count);

        return ResolveDocumentNames(categoryPath, all);
    }

    private string CasePath(string caseId)
    {
        var normalizedCaseId = CasePathResolver.NormalizeCaseId(caseId);
        return $"{_evidenceRoot}/{_datasetOptions.CasesRelativePath}/{normalizedCaseId}/{_datasetOptions.FabricPrerequisiteSubfolder}";
    }

    private string CategoryPath(string caseId, SignalCategory category) =>
        $"{CasePath(caseId)}/{SignalCategoryFolders.For(category)}";

    private string FilePath(string caseId, SignalCategory category, string fileName) =>
        $"{CategoryPath(caseId, category)}/{fileName}";

    private static IReadOnlyList<string> ResolveDocumentNames(string categoryPath, IReadOnlyList<string> listedPaths)
    {
        var prefix = categoryPath.TrimEnd('/') + "/";

        return listedPaths
            .Select(path => ResolveRelativeDocumentPath(prefix, path))
            .Where(name => name is not null
                           && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                           && !name.StartsWith("SCHEMA", StringComparison.OrdinalIgnoreCase))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    private static string? ResolveRelativeDocumentPath(string categoryPrefix, string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');

        if (normalized.StartsWith(categoryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relative = normalized[categoryPrefix.Length..];
            return string.IsNullOrWhiteSpace(relative) ? null : relative;
        }

        // Some OneLake listings return direct child file names without the parent prefix.
        if (!normalized.Contains('/'))
        {
            return normalized;
        }

        return null;
    }

    private static void ValidateCaseId(string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            throw new ArgumentException("Case id must be provided.", nameof(caseId));
        }
    }
}
