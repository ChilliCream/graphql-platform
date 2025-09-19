using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a GraphQL schema document to the schema builder by loading a document from a delegate.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="loadDocument">
    /// A delegate to load the document.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddDocument(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, CancellationToken, ValueTask<DocumentNode>> loadDocument)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loadDocument);

        return builder.ConfigureSchemaAsync(async (sp, b, ct) =>
        {
            var document = await loadDocument(sp, ct).ConfigureAwait(false);
            b.AddDocument(document);
        });
    }

    /// <summary>
    /// Adds a GraphQL schema document to the schema builder by loading a document from a delegate.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="document">
    /// The document.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddDocument(
        this IRequestExecutorBuilder builder,
        DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(document);

        return builder.ConfigureSchema(b => b.AddDocument(document));
    }

    /// <summary>
    /// Adds a GraphQL schema document to the schema builder by loading a document from a string.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="sourceText">
    /// The source text.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder AddDocumentFromString(
        this IRequestExecutorBuilder builder,
        [StringSyntax("graphql")] string sourceText)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(sourceText);

        return builder.ConfigureSchema(b => b.AddDocumentFromString(sourceText));
    }

    /// <summary>
    /// Adds a GraphQL schema document to the schema builder by loading a document from a file.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="filePath">
    /// The file path.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The file path is null or empty.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// The file does not exist.
    /// </exception>
    public static IRequestExecutorBuilder AddDocumentFromFile(
        this IRequestExecutorBuilder builder,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }

        return builder.ConfigureSchema(b => b.AddDocumentFromFile(filePath));
    }
}
