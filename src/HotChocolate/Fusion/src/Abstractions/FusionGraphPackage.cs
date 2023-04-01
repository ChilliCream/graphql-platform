using System.Buffers;
using System.IO.Packaging;
using System.Net.Mime;
using System.Text;
using HotChocolate.Fusion.Composition;
using HotChocolate.Language;
using static HotChocolate.Fusion.FusionGraphPackageConstants;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public sealed class FusionGraphPackage : IDisposable, IAsyncDisposable
{
    private readonly Package _package;

    private FusionGraphPackage(Package package)
    {
        _package = package;
    }

    public static FusionGraphPackage Open(
        Stream stream,
        FileAccess access = FileAccess.ReadWrite)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (access != FileAccess.Read && access != FileAccess.ReadWrite)
        {
            throw new ArgumentOutOfRangeException(nameof(access));
        }

        var package = Package.Open(stream, FileMode.Open, access);
        return new FusionGraphPackage(package);
    }

    public static FusionGraphPackage Open(
        string path,
        FileAccess access = FileAccess.ReadWrite)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (access != FileAccess.Read && access != FileAccess.ReadWrite)
        {
            throw new ArgumentOutOfRangeException(nameof(access));
        }

        var package = Package.Open(path, FileMode.Open, access);
        return new FusionGraphPackage(package);
    }

    public static FusionGraphPackage Create(
        Stream stream)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var package = Package.Open(stream, FileMode.Create, FileAccess.ReadWrite);
        return new FusionGraphPackage(package);
    }

    public static FusionGraphPackage Create(
        string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var package = Package.Open(path, FileMode.Create, FileAccess.ReadWrite);
        return new FusionGraphPackage(package);
    }

    public Task<DocumentNode> GetFusionGraphAsync(
        CancellationToken cancellationToken = default)
    {
        if ((_package.FileOpenAccess & FileAccess.Read) == FileAccess.Read)
        {
            throw new FusionGraphPackageException(
                "The fusion graph package must be opened in read mode to read contents.");
        }

        if (_package.RelationshipExists(FusionId))
        {
            throw new FusionGraphPackageException(
                "This package does not contain a fusion graph document.");
        }

        var relationship = _package.GetRelationship(FusionId);
        var part = _package.GetPart(relationship.TargetUri);
        return ReadSchemaPartAsync(part, cancellationToken);
    }

    public Task SetFusionGraphAsync(
        DocumentNode document,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (_package.FileOpenAccess != FileAccess.ReadWrite)
        {
            throw new FusionGraphPackageException(
                "The fusion graph package must be opened in read/write mode to update contents.");
        }

        if (_package.RelationshipExists(FusionId))
        {
            var relationship = _package.GetRelationship(FusionId);
            _package.DeletePart(relationship.TargetUri);
            _package.DeleteRelationship(relationship.Id);
        }

        return WriteSchemaPartAsync(
            FusionFileName,
            FusionKind,
            FusionId,
            document,
            cancellationToken);
    }

    public Task<DocumentNode> GetSchemaAsync(
        CancellationToken cancellationToken = default)
    {
        if ((_package.FileOpenAccess & FileAccess.Read) == FileAccess.Read)
        {
            throw new FusionGraphPackageException(
                "The fusion graph package must be opened in read mode to read contents.");
        }

        if (_package.RelationshipExists(SchemaId))
        {
            throw new FusionGraphPackageException(
                "This package does not contain a schema document.");
        }

        var relationship = _package.GetRelationship(SchemaId);
        var part = _package.GetPart(relationship.TargetUri);
        return ReadSchemaPartAsync(part, cancellationToken);
    }

    public Task SetSchemaAsync(
        DocumentNode document,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (_package.FileOpenAccess != FileAccess.ReadWrite)
        {
            throw new FusionGraphPackageException(
                "The fusion graph package must be opened in read/write mode to update contents.");
        }

        if (_package.RelationshipExists(FusionId))
        {
            var relationship = _package.GetRelationship(FusionId);
            _package.DeletePart(relationship.TargetUri);
            _package.DeleteRelationship(relationship.Id);
        }

        return WriteSchemaPartAsync(
            SchemaFileName,
            SchemaKind,
            SchemaId,
            document,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SubgraphConfiguration>> GetSubgraphConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        var configurations = new List<SubgraphConfiguration>();

        foreach (var relationship in _package.GetRelationshipsByType(SubgraphConfigKind))
        {
            var part = _package.GetPart(relationship.TargetUri);
            var configuration = await ReadSubgraphConfigurationAsync(part, cancellationToken);
            configurations.Add(configuration);
        }

        return configurations;
    }

    public Task<SubgraphConfiguration?> TryGetSubgraphConfigurationAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if ((_package.FileOpenAccess & FileAccess.Read) == FileAccess.Read)
        {
            throw new FusionGraphPackageException(
                "The fusion graph package must be opened in read mode to read contents.");
        }

        if (_package.RelationshipExists(name))
        {
            return Task.FromResult<SubgraphConfiguration?>(null);
        }

        var relationship = _package.GetRelationship(name);
        var part = _package.GetPart(relationship.TargetUri);
        return ReadSubgraphConfigurationAsync(part, cancellationToken)!;
    }

    public Task SetSubgraphConfigurationAsync(
        SubgraphConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (_package.FileOpenAccess != FileAccess.ReadWrite)
        {
            throw new FusionGraphPackageException(
                "The fusion graph package must be opened in read/write mode to update contents.");
        }

        if (_package.RelationshipExists(configuration.Name))
        {
            var rootRel = _package.GetRelationship(configuration.Name);
            var rootPart = _package.GetPart(rootRel.TargetUri);

            foreach (var relationship in rootPart.GetRelationships())
            {
                _package.DeletePart(relationship.TargetUri);
            }

            _package.DeleteRelationship(configuration.Name);
            _package.DeletePart(rootPart.Uri);
        }

        return WriteSubgraphConfigurationAsync(configuration, cancellationToken);
    }

    private static async Task<DocumentNode> ReadSchemaPartAsync(
        PackagePart schemaPart,
        CancellationToken ct)
    {
        await using var stream = schemaPart.GetStream(FileMode.Open, FileAccess.Read);
        var buffer = new ArrayBufferWriter<byte>();
        int read;

        do
        {
            read = await stream.ReadAsync(buffer.GetMemory(256), ct);
        } while (read > 0);

        return Parse(buffer.WrittenSpan);
    }

    private async Task WriteSchemaPartAsync(
        string fileName,
        string relKind,
        string relId,
        DocumentNode document,
        CancellationToken ct)
    {
        var uri = PackUriHelper.CreatePartUri(new Uri(fileName, UriKind.Relative));
        var part = _package.CreatePart(uri, SchemaMediaType);

        await using var stream = part.GetStream(FileMode.Create);
        var sourceText = Encoding.UTF8.GetBytes(document.ToString(true));
        await stream.WriteAsync(sourceText, ct);

        _package.CreateRelationship(part.Uri, TargetMode.Internal, relKind, relId);
    }

    private async Task<SubgraphConfiguration> ReadSubgraphConfigurationAsync(
        PackagePart rootPart,
        CancellationToken ct)
    {
        var config = await ReadSubgraphConfigurationJsonPartAsync(rootPart, ct);
        var schema = await ReadSubgraphSchemaPartAsync(rootPart, ct);
        var extensions = await ReadSubgraphExtensionPartsAsync(rootPart, ct);

        return new SubgraphConfiguration(
            config.Name,
            schema.ToString(true),
            extensions.Select(t => t.ToString(true)).ToArray(),
            config.Clients);
    }

    private async Task<SubgraphConfigJson> ReadSubgraphConfigurationJsonPartAsync(
        PackagePart rootPart,
        CancellationToken ct)
    {
        await using var stream = rootPart.GetStream(FileMode.Open, FileAccess.Read);
        return await SubgraphConfigJsonSerializer.ParseAsync(stream, ct);
    }

    private async Task<DocumentNode> ReadSubgraphSchemaPartAsync(
        PackagePart rootPart,
        CancellationToken ct)
    {
        var relationship = rootPart.GetRelationship(SchemaId);
        var part = _package.GetPart(relationship.TargetUri);
        return await ReadSchemaPartAsync(part, ct);
    }

    private async Task<IReadOnlyList<DocumentNode>> ReadSubgraphExtensionPartsAsync(
        PackagePart rootPart,
        CancellationToken ct)
    {
        var extensions = new List<DocumentNode>();

        foreach (var relationship in rootPart.GetRelationshipsByType(ExtensionKind))
        {
            var part = _package.GetPart(relationship.TargetUri);
            var extension = await ReadSchemaPartAsync(part, ct);
            extensions.Add(extension);
        }

        return extensions;
    }

    private async Task WriteSubgraphConfigurationAsync(
        SubgraphConfiguration configuration,
        CancellationToken ct)
    {
        var schema = Parse(configuration.Schema);
        var extensions = configuration.Extensions.Select(Parse).ToList();

        var root = await WriteSubgraphConfigurationJsonPartAsync(configuration, ct);
        await WriteSubgraphSchemaPartAsync(configuration.Name, root, schema, ct);
        await WriteSubgraphExtensionPartsAsync(configuration.Name, root, extensions, ct);
    }

    private async Task<PackagePart> WriteSubgraphConfigurationJsonPartAsync(
        SubgraphConfiguration configuration,
        CancellationToken ct)
    {
        var config = new SubgraphConfigJson(
            configuration.Name,
            configuration.Clients);

        var path = $"{configuration.Name}/{SubgraphConfigFileName}";
        var uri = PackUriHelper.CreatePartUri(new Uri(path, UriKind.Relative));
        var part = _package.CreatePart(uri, MediaTypeNames.Application.Json);

        await using var stream = part.GetStream(FileMode.Create);
        await SubgraphConfigJsonSerializer.FormatAsync(config, stream, ct);

        _package.CreateRelationship(part.Uri, TargetMode.Internal, SubgraphConfigKind, config.Name);

        return part;
    }

    private async Task WriteSubgraphSchemaPartAsync(
        string subgraphName,
        PackagePart root,
        DocumentNode document,
        CancellationToken ct)
    {
        var path = $"{subgraphName}/{SchemaFileName}";
        var uri = PackUriHelper.CreatePartUri(new Uri(path, UriKind.Relative));
        var part = _package.CreatePart(uri, SchemaMediaType);

        await using var stream = part.GetStream(FileMode.Create);
        var sourceText = Encoding.UTF8.GetBytes(document.ToString(true));
        await stream.WriteAsync(sourceText, ct);

        root.CreateRelationship(part.Uri, TargetMode.Internal, SchemaKind, SchemaId);
    }

    private async Task WriteSubgraphExtensionPartsAsync(
        string subgraphName,
        PackagePart root,
        IReadOnlyList<DocumentNode> extensions,
        CancellationToken ct)
    {
        for (var i = 0; i < extensions.Count; i++)
        {
            var extension = extensions[i];
            var path = $"{subgraphName}/schema.extension.{i}.graphql";
            var uri = PackUriHelper.CreatePartUri(new Uri(path, UriKind.Relative));
            var part = _package.CreatePart(uri, SchemaMediaType);

            await using var stream = part.GetStream(FileMode.Create);
            var sourceText = Encoding.UTF8.GetBytes(extension.ToString(true));
            await stream.WriteAsync(sourceText, ct);

            root.CreateRelationship(part.Uri, TargetMode.Internal, ExtensionKind);
        }
    }

    public void Dispose()
    {
        _package.Flush();
        _package.Close();
    }

    public ValueTask DisposeAsync()
    {
        _package.Flush();
        _package.Close();
        return default;
    }
}
