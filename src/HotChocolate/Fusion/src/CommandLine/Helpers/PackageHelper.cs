using System.Buffers;
using System.IO.Packaging;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Composition;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using static System.IO.Packaging.PackUriHelper;
using static System.IO.Path;
using static System.UriKind;

namespace HotChocolate.Fusion.CommandLine.Helpers;

internal static class PackageHelper
{
    private const string _schemaKind = "urn:graphql:schema";
    private const string _schemaId = "schema";
    private const string _schemaExtensionKind = "urn:graphql:schema-extensions";
    private const string _subgraphConfigKind = "urn:hotchocolate:fusion:subgraph-config";
    private const string _subgraphConfigId = "subgraph-config";

    private static readonly SyntaxSerializerOptions _serializerOptions =
        new()
        {
            Indented = true,
            MaxDirectivesPerLine = 0
        };

    public static async Task CreateSubgraphPackageAsync(
        string packageFile,
        SubgraphFiles subgraphFiles,
        CancellationToken ct = default)
    {
        if (File.Exists(packageFile))
        {
            File.Delete(packageFile);
        }

        var schema = await LoadSchemaDocumentAsync(subgraphFiles.SchemaFile, ct);
        var transportConfig = await LoadSubgraphConfigAsync(subgraphFiles.SubgraphConfigFile, ct);
        var extensions = new List<DocumentNode>();

        foreach (var extensionFile in subgraphFiles.ExtensionFiles)
        {
            extensions.Add(await LoadSchemaDocumentAsync(extensionFile, ct));
        }

        using var package = Package.Open(packageFile, FileMode.Create);
        await AddSchemaToPackageAsync(package, schema);
        await AddTransportConfigToPackage(package, transportConfig);
        await AddSchemaExtensionsToPackage(package, extensions);
    }

    public static async Task<SubgraphConfigurationDto> LoadSubgraphConfigFromSubgraphPackageAsync(
        string packageFile,
        CancellationToken ct = default)
    {
        using var package = Package.Open(packageFile, FileMode.Open, FileAccess.Read);
        var transportConfig = await ReadSubgraphConfigPartAsync(package, ct);
        return transportConfig;
    }

    public static async Task ReplaceSubgraphConfigInSubgraphPackageAsync(
        string packageFile,
        SubgraphConfigurationDto config)
    {
        using var package = Package.Open(packageFile, FileMode.Open, FileAccess.ReadWrite);
        await ReplaceTransportConfigInPackageAsync(package, config);
    }

    public static async Task CreateSubgraphPackageAsync(
        Stream stream,
        SubgraphConfiguration config)
    {
        var schema = Utf8GraphQLParser.Parse(config.Schema);
        var transportConfig = new SubgraphConfigurationDto(config.Name, config.Clients);
        var extensions = new List<DocumentNode>();

        foreach (var extension in config.Extensions)
        {
            extensions.Add(Utf8GraphQLParser.Parse(extension));
        }

        using Package package = Package.Open(stream, FileMode.Create);
        await AddSchemaToPackageAsync(package, schema);
        await AddTransportConfigToPackage(package, transportConfig);
        await AddSchemaExtensionsToPackage(package, extensions);
    }

    public static async Task<SubgraphConfiguration> ReadSubgraphPackageAsync(
        string packageFile,
        CancellationToken cancellationToken = default)
    {
        using var package = Package.Open(packageFile, FileMode.Open, FileAccess.Read);
        return await ReadSubgraphPackageAsync(package, cancellationToken);
    }

    public static async Task<SubgraphConfiguration> ReadSubgraphPackageAsync(
        Package package,
        CancellationToken cancellationToken = default)
    {
        var schema = await ReadSchemaPartAsync(package, cancellationToken);
        var subgraphConfig = await ReadSubgraphConfigPartAsync(package, cancellationToken);
        var extensions = await ReadSchemaExtensionPartsAsync(package, cancellationToken);

        return new SubgraphConfiguration(
            subgraphConfig.Name,
            schema.ToString(_serializerOptions),
            extensions.Select(t => t.ToString(_serializerOptions)).ToArray(),
            subgraphConfig.Clients,
            subgraphConfig.Extensions);
    }

    public static async Task ExtractSubgraphPackageAsync(
        string packageFile,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(targetDirectory))
        {
            if (Directory.EnumerateFiles(targetDirectory, "*.*", SearchOption.AllDirectories).Any())
            {
                throw new ArgumentException(
                    "The target directory must be empty.",
                    nameof(targetDirectory));
            }
        }
        else
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var config = await ReadSubgraphPackageAsync(packageFile, cancellationToken);
        var subgraphConfigJson = FormatSubgraphConfig(new(config.Name, config.Clients));

        await File.WriteAllTextAsync(
            Combine(targetDirectory, Defaults.SchemaFile),
            config.Schema,
            cancellationToken);

        await File.WriteAllTextAsync(
            Combine(targetDirectory, Defaults.ConfigFile),
            subgraphConfigJson,
            cancellationToken);

        for (var i = 0; i < config.Extensions.Count; i++)
        {
            await File.WriteAllTextAsync(
                Combine(targetDirectory, $"extension-{i}.graphql"),
                config.Extensions[i],
                cancellationToken);
        }
    }

    private static async Task<DocumentNode> LoadSchemaDocumentAsync(
        string filename,
        CancellationToken ct)
    {
        var sourceText = await File.ReadAllBytesAsync(filename, ct);
        return Utf8GraphQLParser.Parse(sourceText);
    }

    public static async Task<SubgraphConfigurationDto> LoadSubgraphConfigAsync(
        string filename,
        CancellationToken ct)
    {
        await using var stream = File.OpenRead(filename);
        return await ParseSubgraphConfigAsync(stream, ct);
    }

    private static HttpClientConfiguration ReadHttpClientConfiguration(
        JsonElement element)
    {
        var baseAddress = new Uri(element.GetProperty("baseAddress").GetString()!);
        var clientName = default(string?);

        if (element.TryGetProperty("clientName", out var clientNameElement))
        {
            clientName = clientNameElement.GetString();
        }

        return new HttpClientConfiguration(baseAddress, clientName);
    }

    private static WebSocketClientConfiguration ReadWebSocketClientConfiguration(
        JsonElement element)
    {
        var baseAddress = new Uri(element.GetProperty("baseAddress").GetString()!);
        var clientName = default(string?);

        if (element.TryGetProperty("clientName", out var clientNameElement))
        {
            clientName = clientNameElement.GetString();
        }

        return new WebSocketClientConfiguration(baseAddress, clientName);
    }

    private static async Task<SubgraphConfigurationDto> ParseSubgraphConfigAsync(
        Stream stream,
        CancellationToken ct)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var configs = new List<IClientConfiguration>();
        var subgraph = default(string?);
        var extensions = default(JsonElement?);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            switch (property.Name)
            {
                case "subgraph":
                    subgraph = property.Value.GetString();
                    break;

                case "http":
                    configs.Add(ReadHttpClientConfiguration(property.Value));
                    break;

                case "websocket":
                    configs.Add(ReadWebSocketClientConfiguration(property.Value));
                    break;

                case "extensions":
                    extensions = property.Value.SafeClone();
                    break;

                default:
                    throw new NotSupportedException(
                        $"Configuration property `{property.Value}` is not supported.");
            }
        }

        if (string.IsNullOrEmpty(subgraph))
        {
            throw new InvalidOperationException("No subgraph name was specified.");
        }

        return new SubgraphConfigurationDto(subgraph, configs, extensions);
    }

    public static string FormatSubgraphConfig(
        SubgraphConfigurationDto subgraphConfig)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WriteString("subgraph", subgraphConfig.Name);

        foreach (var client in subgraphConfig.Clients)
        {
            switch (client)
            {
                case HttpClientConfiguration config:
                    writer.WriteStartObject("http");
                    writer.WriteString("baseAddress", config.BaseAddress.ToString());

                    if (config.ClientName is not null)
                    {
                        writer.WriteString("clientName", config.ClientName);
                    }

                    writer.WriteEndObject();
                    break;

                case WebSocketClientConfiguration config:
                    writer.WriteStartObject("websocket");
                    writer.WriteString("baseAddress", config.BaseAddress.ToString());

                    if (config.ClientName is not null)
                    {
                        writer.WriteString("clientName", config.ClientName);
                    }

                    writer.WriteEndObject();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(client));
            }
        }

        if (subgraphConfig.Extensions is not null)
        {
            writer.WritePropertyName("extensions");
            subgraphConfig.Extensions.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static async Task AddSchemaToPackageAsync(
        Package package,
        DocumentNode schema)
    {
        var uri = CreatePartUri(new Uri("schema.graphql", Relative));
        var part = package.CreatePart(uri, "application/graphql-schema");

        await using var stream = part.GetStream(FileMode.Create);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(schema.ToString(_serializerOptions));

        package.CreateRelationship(part.Uri, TargetMode.Internal, _schemaKind, _schemaId);
    }

    private static async Task<DocumentNode> ReadSchemaPartAsync(
        Package package,
        CancellationToken ct)
    {
        var schemaRel = package.GetRelationship(_schemaId);
        var schemaPart = package.GetPart(schemaRel.TargetUri);

        await using var stream = schemaPart.GetStream(FileMode.Open);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var schema = await reader.ReadToEndAsync(ct);
        return Utf8GraphQLParser.Parse(schema);
    }

    private static async Task AddSchemaExtensionsToPackage(
        Package package,
        List<DocumentNode> extensions)
    {
        for (var i = 0; i < extensions.Count; i++)
        {
            var extension = extensions[i];
            var uri = CreatePartUri(new Uri($"extensions/{i}.graphql", Relative));
            var part = package.CreatePart(uri, "application/graphql-schema");

            await using var stream = part.GetStream(FileMode.Create);
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(extension.ToString(_serializerOptions));

            package.CreateRelationship(part.Uri, TargetMode.Internal, _schemaExtensionKind);
        }
    }

    private static async Task<IReadOnlyList<DocumentNode>> ReadSchemaExtensionPartsAsync(
        Package package,
        CancellationToken ct)
    {
        var list = new List<DocumentNode>();

        foreach (var extensionRel in package.GetRelationshipsByType(_schemaExtensionKind))
        {
            var schemaPart = package.GetPart(extensionRel.TargetUri);
            await using var stream = schemaPart.GetStream(FileMode.Open);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var schema = await reader.ReadToEndAsync(ct);
            list.Add(Utf8GraphQLParser.Parse(schema));
        }

        return list;
    }

    private static async Task AddTransportConfigToPackage(
        Package package,
        SubgraphConfigurationDto subgraphConfig)
    {
        var uri = CreatePartUri(new Uri("subgraph.json", Relative));
        var part = package.CreatePart(uri, MediaTypeNames.Application.Json);

        await using var stream = part.GetStream(FileMode.Create);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(FormatSubgraphConfig(subgraphConfig));

        package.CreateRelationship(
            part.Uri,
            TargetMode.Internal,
            _subgraphConfigKind,
            _subgraphConfigId);
    }

    private static async Task ReplaceTransportConfigInPackageAsync(
        Package package,
        SubgraphConfigurationDto subgraphConfig)
    {
        var uri = CreatePartUri(new Uri("subgraph.json", Relative));
        var part = package.GetPart(uri);

        await using var stream = part.GetStream(FileMode.Create);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(FormatSubgraphConfig(subgraphConfig));
    }

    private static async Task<SubgraphConfigurationDto> ReadSubgraphConfigPartAsync(
        Package package,
        CancellationToken ct)
    {
        var subgraphConfigRel = package.GetRelationship(_subgraphConfigId);
        var subgraphConfigPart = package.GetPart(subgraphConfigRel.TargetUri);

        await using var stream = subgraphConfigPart.GetStream(FileMode.Open);
        return await ParseSubgraphConfigAsync(stream, ct);
    }
}
