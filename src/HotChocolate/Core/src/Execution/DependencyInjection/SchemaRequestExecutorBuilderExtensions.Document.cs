using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddDocument(
        this IRequestExecutorBuilder builder,
        LoadDocumentAsync loadDocumentAsync)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (loadDocumentAsync is null)
        {
            throw new ArgumentNullException(nameof(loadDocumentAsync));
        }

        return builder.ConfigureSchemaAsync(async (sp, b, ct) =>
        {
            var document = await loadDocumentAsync(sp, ct).ConfigureAwait(false);
            b.AddDocument(document);
        });
    }

    public static IRequestExecutorBuilder AddDocument(
        this IRequestExecutorBuilder builder,
        DocumentNode document)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return builder.ConfigureSchema(b => b.AddDocument(document));
    }

    public static IRequestExecutorBuilder AddDocumentFromString(
        this IRequestExecutorBuilder builder,
        string sdl)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (sdl is null)
        {
            throw new ArgumentNullException(nameof(sdl));
        }

        return builder.ConfigureSchema(b => b.AddDocumentFromString(sdl));
    }

    public static IRequestExecutorBuilder AddDocumentFromFile(
        this IRequestExecutorBuilder builder,
        string filePath)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException(nameof(filePath));
        }

        return builder.AddDocument(async (_, ct) =>
        {
            var buffer = await Task
                .Run(() => File.ReadAllBytes(filePath), ct)
                .ConfigureAwait(false);
            return Utf8GraphQLParser.Parse(buffer);
        });
    }
}
