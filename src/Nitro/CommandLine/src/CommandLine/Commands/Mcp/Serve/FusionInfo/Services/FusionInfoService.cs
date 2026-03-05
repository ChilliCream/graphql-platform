using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Models;
using ChilliCream.Nitro.CommandLine.FusionCompatibility;
using HotChocolate.Language;
using StrawberryShake;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Services;

internal sealed class FusionInfoService(IApiClient client, IHttpClientFactory httpClientFactory)
{
    public async Task<FusionInfoResult> GetFusionInfoAsync(string apiId, string stage, CancellationToken ct = default)
    {
        // 1. Fetch metadata to get the tag
        var metaResult = await client.FetchConfiguration.ExecuteAsync(apiId, stage, ct);
        metaResult.EnsureNoErrors();

        var meta =
            metaResult.Data?.FusionConfigurationByApiId
            ?? throw FusionInfoThrowHelper.FusionConfigurationNotFound(apiId, stage);

        var tag = meta.Tag;

        // 2. Determine extraction directory and check cache
        var extractDir = GetExtractionDirectory(apiId, stage);
        var cacheTagFile = Path.Combine(extractDir, ".nitro-cache-tag");

        if (!IsCacheValid(cacheTagFile, tag))
        {
            // 3. Download FGP archive
            await using var archiveStream =
                await FusionPublishHelpers.DownloadLatestFusionArchiveAsync(apiId, stage, client, httpClientFactory, ct)
                ?? throw FusionInfoThrowHelper.FusionArchiveDownloadFailed(apiId, stage);

            // 4. Extract to disk
            await ExtractArchiveAsync(archiveStream, extractDir, ct);

            // 5. Write cache tag
            await File.WriteAllTextAsync(cacheTagFile, tag, ct);
        }

        // 6. Build result from extracted files
        return await BuildResultFromDiskAsync(tag, extractDir, ct);
    }

    internal static string GetExtractionDirectory(string apiId, string stage)
    {
        return Path.Combine(Path.GetTempPath(), "nitro-fusion-" + SanitizeName(apiId) + "-" + SanitizeName(stage));
    }

    internal static bool IsCacheValid(string cacheTagFile, string currentTag)
    {
        if (!File.Exists(cacheTagFile))
        {
            return false;
        }

        var cachedTag = File.ReadAllText(cacheTagFile).Trim();
        return string.Equals(cachedTag, currentTag, StringComparison.Ordinal);
    }

    private static async Task ExtractArchiveAsync(Stream archiveStream, string extractDir, CancellationToken ct)
    {
        // Clear any stale extraction
        if (Directory.Exists(extractDir))
        {
            Directory.Delete(extractDir, recursive: true);
        }

        Directory.CreateDirectory(extractDir);

        // FusionGraphPackage needs a seekable stream
        await using var ms = new MemoryStream();
        await archiveStream.CopyToAsync(ms, ct);
        ms.Position = 0;

        await using var pkg = FusionGraphPackage.Open(ms, FileAccess.Read);

        // Write fusion.graphql (composed schema with fusion directives)
        try
        {
            var fusionGraph = await pkg.GetFusionGraphAsync(ct);
            var fusionGraphPath = Path.Combine(extractDir, "fusion.graphql");
            await File.WriteAllTextAsync(fusionGraphPath, fusionGraph.ToString(true), ct);
        }
        catch (FusionGraphPackageException)
        {
            // fusion.graphql is optional in some archive formats
        }

        // Write schema.graphql (public schema without fusion directives)
        try
        {
            var publicSchema = await pkg.GetSchemaAsync(ct);
            var schemaPath = Path.Combine(extractDir, "schema.graphql");
            await File.WriteAllTextAsync(schemaPath, publicSchema.ToString(true), ct);
        }
        catch (FusionGraphPackageException)
        {
            // schema.graphql may not be present in all versions
        }

        // Write each subgraph's files
        var subgraphs = await pkg.GetSubgraphConfigurationsAsync(ct);

        foreach (var subgraph in subgraphs)
        {
            var subgraphDir = Path.Combine(extractDir, subgraph.Name);
            Directory.CreateDirectory(subgraphDir);

            // Write schema.graphql
            var subSchemaPath = Path.Combine(subgraphDir, "schema.graphql");
            await File.WriteAllTextAsync(subSchemaPath, subgraph.Schema, ct);

            // Write subgraph-config.json (reconstructed)
            var configPath = Path.Combine(subgraphDir, "subgraph-config.json");
            await WriteSubgraphConfigJsonAsync(subgraph, configPath, ct);
        }
    }

    private static async Task WriteSubgraphConfigJsonAsync(
        SubgraphConfiguration subgraph,
        string path,
        CancellationToken ct)
    {
        await using var stream = File.Create(path);
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("name", subgraph.Name);
        writer.WriteStartArray("clients");

        foreach (var client in subgraph.Clients.OfType<HttpClientConfiguration>())
        {
            writer.WriteStartObject();
            writer.WriteString("kind", "Default");
            writer.WriteString("baseAddress", client.BaseAddress.ToString());
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();

        await writer.FlushAsync(ct);
    }

    private static async Task<FusionInfoResult> BuildResultFromDiskAsync(
        string tag,
        string extractDir,
        CancellationToken ct)
    {
        var composedSchemaPath = Path.Combine(extractDir, "fusion.graphql");
        var subgraphSchemaPaths = new Dictionary<string, string>();
        var subgraphInfos = new List<SubgraphInfo>();

        var totalTypes = 0;
        var totalFields = 0;

        // Collect composed schema stats
        if (File.Exists(composedSchemaPath))
        {
            var composedSdl = await File.ReadAllTextAsync(composedSchemaPath, ct);
            var composedDoc = Utf8GraphQLParser.Parse(composedSdl);
            var (t, f) = CountSchemaCoordinates(composedDoc);
            totalTypes = t;
            totalFields = f;
        }

        // Enumerate subgraph directories (bounded to prevent DoS)
        const int maxSubgraphs = 200;
        var subgraphDirs = Directory.GetDirectories(extractDir);
        foreach (var subgraphDir in subgraphDirs.Take(maxSubgraphs))
        {
            var schemaPath = Path.Combine(subgraphDir, "schema.graphql");
            if (!File.Exists(schemaPath))
            {
                continue;
            }

            var subgraphName = Path.GetFileName(subgraphDir);
            subgraphSchemaPaths[subgraphName] = schemaPath;

            var schemaSdl = await File.ReadAllTextAsync(schemaPath, ct);
            var schemaDoc = Utf8GraphQLParser.Parse(schemaSdl);

            var (schemaCoordinateCount, _) = CountSchemaCoordinates(schemaDoc);
            var rootTypes = ExtractRootTypes(schemaDoc);

            // Read endpoint URL from subgraph-config.json
            string? endpointUrl = null;
            var configPath = Path.Combine(subgraphDir, "subgraph-config.json");
            if (File.Exists(configPath))
            {
                endpointUrl = await ReadEndpointUrlAsync(configPath, ct);
            }

            subgraphInfos.Add(
                new SubgraphInfo
                {
                    Name = subgraphName,
                    EndpointUrl = endpointUrl,
                    SchemaCoordinateCount = schemaCoordinateCount,
                    RootTypes = rootTypes
                });
        }

        return new FusionInfoResult
        {
            Tag = tag,
            Subgraphs = subgraphInfos,
            ComposedSchemaPath = composedSchemaPath,
            SubgraphSchemaPaths = subgraphSchemaPaths,
            TotalTypes = totalTypes,
            TotalFields = totalFields
        };
    }

    private static async Task<string?> ReadEndpointUrlAsync(string configPath, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(configPath, ct);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("clients", out var clients)
                && clients.ValueKind == JsonValueKind.Array)
            {
                foreach (var client in clients.EnumerateArray())
                {
                    if (client.TryGetProperty("baseAddress", out var addr))
                    {
                        return addr.GetString();
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or FormatException)
        {
            // Best-effort; return null on any parsing failure
        }

        return null;
    }

    private static (int Types, int Fields) CountSchemaCoordinates(DocumentNode doc)
    {
        var types = 0;
        var fields = 0;

        foreach (var def in doc.Definitions)
        {
            switch (def)
            {
                case ObjectTypeDefinitionNode t:
                    types++;
                    fields += t.Fields.Count;
                    break;
                case InterfaceTypeDefinitionNode t:
                    types++;
                    fields += t.Fields.Count;
                    break;
                case InputObjectTypeDefinitionNode t:
                    types++;
                    fields += t.Fields.Count;
                    break;
                case UnionTypeDefinitionNode:
                case EnumTypeDefinitionNode:
                case ScalarTypeDefinitionNode:
                    types++;
                    break;
            }
        }

        return (types, fields);
    }

    private static SubgraphRootTypes ExtractRootTypes(DocumentNode doc)
    {
        var queryName = "Query";
        var mutationName = "Mutation";
        var subscriptionName = "Subscription";

        foreach (var def in doc.Definitions.OfType<SchemaDefinitionNode>())
        {
            foreach (var op in def.OperationTypes)
            {
                switch (op.Operation)
                {
                    case OperationType.Query:
                        queryName = op.Type.Name.Value;
                        break;
                    case OperationType.Mutation:
                        mutationName = op.Type.Name.Value;
                        break;
                    case OperationType.Subscription:
                        subscriptionName = op.Type.Name.Value;
                        break;
                }
            }
        }

        var queryFields = new List<string>();
        var mutationFields = new List<string>();
        var subscriptionFields = new List<string>();

        foreach (var def in doc.Definitions.OfType<ObjectTypeDefinitionNode>())
        {
            if (def.Name.Value == queryName)
            {
                queryFields.AddRange(def.Fields.Select(f => f.Name.Value));
            }
            else if (def.Name.Value == mutationName)
            {
                mutationFields.AddRange(def.Fields.Select(f => f.Name.Value));
            }
            else if (def.Name.Value == subscriptionName)
            {
                subscriptionFields.AddRange(def.Fields.Select(f => f.Name.Value));
            }
        }

        return new SubgraphRootTypes
        {
            Query = queryFields,
            Mutation = mutationFields,
            Subscription = subscriptionFields
        };
    }

    internal static string SanitizeName(string name)
    {
        var maxLen = Math.Min(name.Length, 40);
        return string.Create(maxLen, name, static (span, src) =>
        {
            var len = Math.Min(src.Length, span.Length);
            for (var i = 0; i < len; i++)
            {
                var c = src[i];
                span[i] = char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-';
            }
        }).Trim('-');
    }
}
