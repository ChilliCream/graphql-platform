using System.Buffers;
using System.IO.Packaging;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Composition;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using static HotChocolate.Fusion.FusionAbstractionResources;
using static HotChocolate.Fusion.FusionGraphPackageConstants;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

/// <summary>
/// <para>
/// A package that contains the Fusion graph document which is used to configure
/// a Fusion GraphQL gateway.
/// </para>
/// <para>
/// Besides the Fusion graph document the package can also contain a schema file and
/// all subgraph configurations it was composed of.
/// </para>
/// </summary>
public sealed class FusionGraphPackage : IDisposable, IAsyncDisposable
{
    private static readonly SyntaxSerializerOptions _syntaxSerializerOptions =
        new()
        {
            Indented = true,
            MaxDirectivesPerLine = 0
        };

    private readonly Package _package;

    private FusionGraphPackage(Package package)
    {
        _package = package;
    }

    /// <summary>
    /// Opens or creates a Fusion graph package.
    /// </summary>
    /// <param name="stream">
    /// The stream that contains the Fusion graph package.
    /// </param>
    /// <param name="access">
    /// The access mode for the Fusion graph package.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stream"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="access"/> is not <see cref="FileAccess.Read"/> or
    /// <see cref="FileAccess.ReadWrite"/>.
    /// </exception>
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

        var package = Package.Open(stream, FileMode.OpenOrCreate, access);
        return new FusionGraphPackage(package);
    }

    /// <summary>
    /// Opens or creates a Fusion graph package.
    /// </summary>
    /// <param name="path">
    /// The path to the Fusion graph package.
    /// </param>
    /// <param name="access">
    /// The access mode for the Fusion graph package.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="access"/> is not <see cref="FileAccess.Read"/> or
    /// <see cref="FileAccess.ReadWrite"/>.
    /// </exception>
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

        var mode = access == FileAccess.Read
            ? FileMode.Open
            : FileMode.OpenOrCreate;
        var package = Package.Open(path, mode, access);
        return new FusionGraphPackage(package);
    }

    /// <summary>
    /// Gets the Fusion graph document to configure the a Fusion GraphQL Gateway.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The Fusion graph document.
    /// </returns>
    /// <exception cref="FusionGraphPackageException">
    /// The package is not readable or the package does not contain a Fusion graph document.
    /// </exception>
    public Task<DocumentNode> GetFusionGraphAsync(
        CancellationToken cancellationToken = default)
    {
        if ((_package.FileOpenAccess & FileAccess.Read) != FileAccess.Read)
        {
            throw new FusionGraphPackageException(FusionGraphPackage_CannotRead);
        }

        if (!_package.RelationshipExists(FusionId))
        {
            throw new FusionGraphPackageException(FusionGraphPackage_NoFusionGraphDoc);
        }

        var relationship = _package.GetRelationship(FusionId);
        var part = _package.GetPart(relationship.TargetUri);
        return ReadSchemaPartAsync(part, cancellationToken);
    }

    /// <summary>
    /// Adds a Fusion graph document to the package or
    /// replaces the Fusion graph document in the package.
    /// </summary>
    /// <param name="document">
    /// The Fusion graph document.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="document"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="FusionGraphPackageException">
    /// The Fusion graph package must be opened in read/write mode to update contents.
    /// </exception>
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
            throw new FusionGraphPackageException(FusionGraphPackage_CannotWrite);
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

    public Task<JsonDocument> GetFusionGraphSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        if ((_package.FileOpenAccess & FileAccess.Read) != FileAccess.Read)
        {
            throw new FusionGraphPackageException(FusionGraphPackage_CannotRead);
        }

        if (!_package.RelationshipExists(FusionSettingsId))
        {
            return Task.FromResult(
                JsonDocument.Parse(
                    """
                    {
                      "fusionTypePrefix" : null,
                      "fusionTypeSelf": false
                    }
                    """));
        }

        var relationship = _package.GetRelationship(FusionSettingsId);
        var part = _package.GetPart(relationship.TargetUri);
        return ReadJsonPartAsync(part, cancellationToken);
    }

    public Task SetFusionGraphSettingsAsync(
        JsonDocument document,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (_package.FileOpenAccess != FileAccess.ReadWrite)
        {
            throw new FusionGraphPackageException(FusionGraphPackage_CannotWrite);
        }

        if (_package.RelationshipExists(FusionSettingsId))
        {
            var relationship = _package.GetRelationship(FusionSettingsId);
            _package.DeletePart(relationship.TargetUri);
            _package.DeleteRelationship(relationship.Id);
        }

        return WriteJsonPartAsync(
            FusionSettingsFileName,
            FusionSettingsKind,
            FusionSettingsId,
            document,
            cancellationToken);
    }

    /// <summary>
    /// Gets the schema document that represents the public schema
    /// the Fusion GraphQL Gateway will expose.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The schema document.
    /// </returns>
    /// <exception cref="FusionGraphPackageException">
    /// The package is not readable or the package does not contain a schema document.
    /// </exception>
    public Task<DocumentNode> GetSchemaAsync(
        CancellationToken cancellationToken = default)
    {
        if ((_package.FileOpenAccess & FileAccess.Read) != FileAccess.Read)
        {
            throw new FusionGraphPackageException(
                FusionGraphPackage_CannotRead);
        }

        if (!_package.RelationshipExists(SchemaId))
        {
            throw new FusionGraphPackageException(
                "This package does not contain a schema document.");
        }

        var relationship = _package.GetRelationship(SchemaId);
        var part = _package.GetPart(relationship.TargetUri);
        return ReadSchemaPartAsync(part, cancellationToken);
    }

    /// <summary>
    /// Adds a schema document to the package or replaces the schema document in the package.
    /// </summary>
    /// <param name="document">
    /// The schema document.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="document"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="FusionGraphPackageException">
    /// The Fusion graph package must be opened in read/write mode to update contents.
    /// </exception>
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
            throw new FusionGraphPackageException(FusionGraphPackage_CannotWrite);
        }

        if (_package.RelationshipExists(SchemaId))
        {
            var relationship = _package.GetRelationship(SchemaId);
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

    /// <summary>
    /// Gets the subgraph configurations that Fusion GraphQL document is composed of.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The subgraph configurations.
    /// </returns>
    /// <exception cref="FusionGraphPackageException">
    /// The package is not readable or the package does not contain any subgraph configurations.
    /// </exception>
    public async Task<IReadOnlyList<SubgraphConfiguration>> GetSubgraphConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        if ((_package.FileOpenAccess & FileAccess.Read) != FileAccess.Read)
        {
            throw new FusionGraphPackageException(FusionGraphPackage_CannotRead);
        }

        var configurations = new List<SubgraphConfiguration>();

        foreach (var relationship in _package.GetRelationshipsByType(SubgraphConfigKind))
        {
            var part = _package.GetPart(relationship.TargetUri);
            var configuration = await ReadSubgraphConfigurationAsync(part, cancellationToken);
            configurations.Add(configuration);
        }

        return configurations;
    }

    /// <summary>
    /// Trues to get a subgraph configuration by name.
    /// </summary>
    /// <param name="name">
    /// The name of the subgraph configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The subgraph configuration or <c>null</c> if the subgraph configuration does not exist.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="FusionGraphPackageException">
    /// The package is not readable.
    /// </exception>
    public Task<SubgraphConfiguration?> TryGetSubgraphConfigurationAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if ((_package.FileOpenAccess & FileAccess.Read) != FileAccess.Read)
        {
            throw new FusionGraphPackageException(FusionGraphPackage_CannotRead);
        }

        if (!_package.RelationshipExists(name))
        {
            return Task.FromResult<SubgraphConfiguration?>(null);
        }

        var relationship = _package.GetRelationship(name);
        var part = _package.GetPart(relationship.TargetUri);
        return ReadSubgraphConfigurationAsync(part, cancellationToken)!;
    }

    /// <summary>
    /// Adds a subgraph configuration to the package or
    /// replaces the subgraph configuration in the package.
    /// </summary>
    /// <param name="configuration">
    /// The subgraph configuration.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configuration"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="FusionGraphPackageException">
    /// The Fusion graph package must be opened in read/write mode to update contents.
    /// </exception>
    public async Task SetSubgraphConfigurationAsync(
        SubgraphConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (_package.FileOpenAccess != FileAccess.ReadWrite)
        {
            throw new FusionGraphPackageException(FusionGraphPackage_CannotWrite);
        }

        await RemoveSubgraphConfigurationAsync(configuration.Name, cancellationToken);

        await WriteSubgraphConfigurationAsync(configuration, cancellationToken);
    }

    /// <summary>
    /// Removes a subgraph configuration from the package.
    /// </summary>
    /// <param name="subgraphName">
    /// The name of the subgraph configuration to remove.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="subgraphName"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="FusionGraphPackageException">
    /// The Fusion graph package must be opened in read/write mode to update contents.
    /// </exception>
    public Task RemoveSubgraphConfigurationAsync(
        string subgraphName,
        CancellationToken cancellationToken = default)
    {
        if (subgraphName is null)
        {
            throw new ArgumentNullException(nameof(subgraphName));
        }

        if (_package.FileOpenAccess != FileAccess.ReadWrite)
        {
            throw new FusionGraphPackageException(FusionGraphPackage_CannotWrite);
        }

        if (_package.RelationshipExists(subgraphName))
        {
            var rootRel = _package.GetRelationship(subgraphName);
            var rootPart = _package.GetPart(rootRel.TargetUri);

            foreach (var relationship in rootPart.GetRelationships())
            {
                _package.DeletePart(relationship.TargetUri);
            }

            _package.DeleteRelationship(subgraphName);
            _package.DeletePart(rootPart.Uri);
        }

        _package.Flush();

        return Task.CompletedTask;
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
            buffer.Advance(read);
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
        var sourceText = Encoding.UTF8.GetBytes(document.ToString(_syntaxSerializerOptions));
        await stream.WriteAsync(sourceText, ct);

        _package.CreateRelationship(part.Uri, TargetMode.Internal, relKind, relId);
        _package.Flush();
    }

    private static async Task<JsonDocument> ReadJsonPartAsync(
        PackagePart schemaPart,
        CancellationToken ct)
    {
        var options = new JsonDocumentOptions { MaxDepth = 16, CommentHandling = JsonCommentHandling.Skip, };
        await using var stream = schemaPart.GetStream(FileMode.Open, FileAccess.Read);
        return await JsonDocument.ParseAsync(stream, options, ct);
    }

    private async Task WriteJsonPartAsync(
        string fileName,
        string relKind,
        string relId,
        JsonDocument document,
        CancellationToken ct)
    {
        var uri = PackUriHelper.CreatePartUri(new Uri(fileName, UriKind.Relative));
        var part = _package.CreatePart(uri, JsonMediaType);

        var options = new JsonWriterOptions { Indented = true, MaxDepth = 16, };
        await using var stream = part.GetStream(FileMode.Create);
        await using var writer = new Utf8JsonWriter(stream, options);
        document.WriteTo(writer);
        await writer.FlushAsync(ct);

        _package.CreateRelationship(part.Uri, TargetMode.Internal, relKind, relId);
        _package.Flush();
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
            extensions.Select(t => t.ToString(_syntaxSerializerOptions)).ToArray(),
            config.Clients,
            config.Extensions);
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
        _package.Flush();
    }

    private async Task<PackagePart> WriteSubgraphConfigurationJsonPartAsync(
        SubgraphConfiguration configuration,
        CancellationToken ct)
    {
        var config = new SubgraphConfigJson(
            configuration.Name,
            configuration.Clients,
            configuration.ConfigurationExtensions);

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
        var sourceText = Encoding.UTF8.GetBytes(document.ToString(_syntaxSerializerOptions));
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
            var sourceText = Encoding.UTF8.GetBytes(extension.ToString(_syntaxSerializerOptions));
            await stream.WriteAsync(sourceText, ct);

            root.CreateRelationship(part.Uri, TargetMode.Internal, ExtensionKind);
        }
    }

    /// <summary>
    /// Disposes the package.
    /// </summary>
    public void Dispose() => _package.Close();

    /// <summary>
    /// Disposes the package.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    public ValueTask DisposeAsync()
    {
        _package.Close();
        return default;
    }
}
