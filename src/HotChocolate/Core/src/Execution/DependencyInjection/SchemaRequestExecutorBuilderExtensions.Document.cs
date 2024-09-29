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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loadDocumentAsync);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(document);

        return builder.ConfigureSchema(b => b.AddDocument(document));
    }

    public static IRequestExecutorBuilder AddDocumentFromString(
        this IRequestExecutorBuilder builder,
        string sdl)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(sdl);

        return builder.ConfigureSchema(b => b.AddDocumentFromString(sdl));
    }

    public static IRequestExecutorBuilder AddDocumentFromFile(
        this IRequestExecutorBuilder builder,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(builder);

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
