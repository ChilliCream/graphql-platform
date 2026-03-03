using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

internal sealed class SchemaStatisticsService
{
    private const int MaxCoordinatesPerBatch = 50;
    private const int MaxTopClients = 10;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, string> _stageIdCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, CachedStats> _statsCache = new(StringComparer.Ordinal);

    public async Task<SchemaStatisticsResult> GetStatisticsAsync(
        NitroApiService apiService,
        SchemaIndex? schemaIndex,
        string apiId,
        string stageName,
        string[] coordinates,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        // 1. Check cache
        var sortedCoords = coordinates.OrderBy(c => c, StringComparer.Ordinal).ToArray();
        var cacheKey = BuildCacheKey(apiId, stageName, from, to, sortedCoords);

        if (_statsCache.TryGetValue(cacheKey, out var cached)
            && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return cached.Result;
        }

        // 2. Resolve stage ID (with caching)
        var stageId = await ResolveStageIdAsync(apiService, apiId, stageName, cancellationToken);
        if (stageId is null)
        {
            throw SchemaThrowHelper.StageNotFound(stageName, apiId);
        }

        // 3. Batch-fetch statistics
        var allStats = new List<CoordinateStatisticsEntry>();
        foreach (var chunk in coordinates.Chunk(MaxCoordinatesPerBatch))
        {
            var chunkStats = await FetchBatchAsync(apiService, stageId, chunk, from, to, cancellationToken);
            allStats.AddRange(chunkStats);
        }

        // 4. Enrich with local schema index (deprecationReason)
        EnrichWithLocalIndex(schemaIndex, allStats);

        // 5. Build result
        var result = new SchemaStatisticsResult
        {
            Statistics = allStats,
            Window = new StatisticsWindow
            {
                From = from.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                To = to.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            Stage = stageName,
            ApiId = apiId
        };

        // 6. Store in cache (with eviction)
        EvictIfNeeded();
        _statsCache[cacheKey] = new CachedStats(result, DateTimeOffset.UtcNow.Add(CacheTtl));

        return result;
    }

    private void EvictIfNeeded()
    {
        const int maxCacheEntries = 500;

        if (_statsCache.Count < maxCacheEntries)
        {
            return;
        }

        // First pass: remove expired entries
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _statsCache.Where(kvp => kvp.Value.ExpiresAt <= now).Select(kvp => kvp.Key).ToList();

        foreach (var key in expiredKeys)
        {
            _statsCache.TryRemove(key, out _);
        }

        if (_statsCache.Count < maxCacheEntries)
        {
            return;
        }

        // Second pass: remove oldest entries until under limit
        var toRemove = _statsCache
            .OrderBy(kvp => kvp.Value.ExpiresAt)
            .Take(_statsCache.Count - maxCacheEntries + 1)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _statsCache.TryRemove(key, out _);
        }
    }

    private async Task<string?> ResolveStageIdAsync(
        NitroApiService apiService,
        string apiId,
        string stageName,
        CancellationToken cancellationToken)
    {
        var key = apiId + ":" + stageName;
        if (_stageIdCache.TryGetValue(key, out var cachedId))
        {
            return cachedId;
        }

        var stageId = await apiService.ResolveStageIdAsync(apiId, stageName, cancellationToken);
        if (stageId is not null)
        {
            _stageIdCache[key] = stageId;
        }

        return stageId;
    }

    private static async Task<List<CoordinateStatisticsEntry>> FetchBatchAsync(
        NitroApiService apiService,
        string stageId,
        string[] coordinates,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var request = CoordinateStatsBatchQueryBuilder.Build(stageId, coordinates, from, to);
        using var operationResult = await apiService.ExecuteGraphQLAsync(request, cancellationToken);

        var results = new List<CoordinateStatisticsEntry>(coordinates.Length);

        var data = operationResult.Data;
        if (data.ValueKind == JsonValueKind.Undefined
            || !data.TryGetProperty("node", out var node)
            || !node.TryGetProperty("coordinates", out var coordArray)
            || coordArray.ValueKind != JsonValueKind.Array)
        {
            foreach (var coord in coordinates)
            {
                results.Add(CreateNotFoundEntry(coord));
            }

            return results;
        }

        // Build a lookup from the returned array keyed by coordinate name.
        var lookup = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var item in coordArray.EnumerateArray())
        {
            if (item.TryGetProperty("coordinate", out var coordProp) && coordProp.GetString() is { } coordName)
            {
                lookup[coordName] = item;
            }
        }

        foreach (var coord in coordinates)
        {
            if (!lookup.TryGetValue(coord, out var member))
            {
                results.Add(CreateNotFoundEntry(coord));
                continue;
            }

            var entry = ParseCoordinateEntry(coord, member);
            results.Add(entry);
        }

        return results;
    }

    private static CoordinateStatisticsEntry CreateNotFoundEntry(string coordinate)
    {
        return new CoordinateStatisticsEntry
        {
            Coordinate = coordinate,
            Found = false,
            IsDeprecated = false,
            DeprecationReason = null,
            Usage = null
        };
    }

    private static CoordinateStatisticsEntry ParseCoordinateEntry(string coordinate, JsonElement member)
    {
        var isDeprecated =
            member.TryGetProperty("isDeprecated", out var depElement) && depElement.ValueKind == JsonValueKind.True;

        CoordinateUsageEntry? usage = null;
        if (member.TryGetProperty("usage", out var usageElement)
            && usageElement.ValueKind != JsonValueKind.Null)
        {
            usage = ParseUsage(usageElement);
        }

        var topClients = new List<TopClientEntry>();
        if (member.TryGetProperty("metrics", out var metrics)
            && metrics.TryGetProperty("clientUsages", out var clientUsages)
            && clientUsages.ValueKind == JsonValueKind.Array)
        {
            var count = 0;
            foreach (var client in clientUsages.EnumerateArray())
            {
                if (count >= MaxTopClients)
                {
                    break;
                }

                topClients.Add(
                    new TopClientEntry
                    {
                        Name = client.TryGetProperty("name", out var n) ? n.GetString() : null,
                        TotalRequests = GetInt64OrDefault(client, "totalRequests"),
                        TotalOperations = GetInt64OrDefault(client, "totalOperations"),
                        TotalVersions = GetInt64OrDefault(client, "totalVersions")
                    });

                count++;
            }
        }

        return new CoordinateStatisticsEntry
        {
            Coordinate = coordinate,
            Found = true,
            IsDeprecated = isDeprecated,
            DeprecationReason = null,
            Usage = usage,
            TopClients = topClients
        };
    }

    private static CoordinateUsageEntry ParseUsage(JsonElement usage)
    {
        return new CoordinateUsageEntry
        {
            ClientCount = GetInt64OrDefault(usage, "clientCount"),
            OperationCount = GetInt64OrDefault(usage, "operationCount"),
            TotalReferences = GetInt64OrDefault(usage, "totalReferences"),
            TotalRequests = GetNullableInt64(usage, "totalRequests"),
            TotalUsages = GetNullableInt64(usage, "totalUsages"),
            Opm = GetNullableDouble(usage, "opm"),
            ErrorRate = GetNullableDouble(usage, "errorRate"),
            MeanDuration = GetNullableDouble(usage, "meanDuration"),
            FirstSeen = GetNullableString(usage, "firstSeen"),
            LastSeen = GetNullableString(usage, "lastSeen")
        };
    }

    private static void EnrichWithLocalIndex(SchemaIndex? schemaIndex, List<CoordinateStatisticsEntry> stats)
    {
        if (schemaIndex is null)
        {
            return;
        }

        foreach (var stat in stats)
        {
            if (!stat.Found)
            {
                continue;
            }

            var indexEntry = schemaIndex.GetByCoordinate(stat.Coordinate);
            if (indexEntry?.DeprecationReason is not null)
            {
                stat.DeprecationReason = indexEntry.DeprecationReason;
            }
        }
    }

    private static string BuildCacheKey(
        string apiId,
        string stageName,
        DateTimeOffset from,
        DateTimeOffset to,
        string[] sortedCoords)
    {
        var raw =
            apiId + "|" + stageName + "|" + from.UtcTicks + "|" + to.UtcTicks + "|" + string.Join(",", sortedCoords);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static long GetInt64OrDefault(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt64();
        }

        return 0;
    }

    private static long? GetNullableInt64(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt64();
        }

        return null;
    }

    private static double? GetNullableDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }

        return null;
    }

    private static string? GetNullableString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }

        return null;
    }

    private sealed record CachedStats(SchemaStatisticsResult Result, DateTimeOffset ExpiresAt);
}
