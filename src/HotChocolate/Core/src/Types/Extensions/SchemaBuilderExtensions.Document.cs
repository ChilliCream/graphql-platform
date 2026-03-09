using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate;

public static partial class SchemaBuilderExtensions
{
    /// <summary>
    /// Adds a GraphQL schema document to the schema builder by parsing a schema string.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="schema">
    /// The GraphQL schema as a UTF-8 string.
    /// </param>
    /// <returns>
    /// The schema builder instance with the document added.
    /// </returns>
    public static ISchemaBuilder AddDocumentFromString(
        this ISchemaBuilder builder,
        [StringSyntax("graphql")] string schema)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(schema);

        return builder.AddDocument(_ => Utf8GraphQLParser.Parse(schema));
    }

    /// <summary>
    /// Adds a GraphQL schema document to the schema builder by parsing a file.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="filePath">
    /// The path to the file containing the GraphQL schema.
    /// </param>
    /// <returns>
    /// The schema builder instance with the document added.
    /// </returns>
    public static ISchemaBuilder AddDocumentFromFile(
        this ISchemaBuilder builder,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        return builder.AddDocument(_ => ParseDocument(filePath));
    }

    /// <summary>
    /// Adds a GraphQL schema document to the schema builder.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="document">
    /// The GraphQL schema document.
    /// </param>
    /// <returns>
    /// The schema builder instance with the document added.
    /// </returns>
    public static ISchemaBuilder AddDocument(
        this ISchemaBuilder builder,
        DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(document);

        var feature = builder.Features.GetOrSet<TypeSystemFeature>();
        feature.SchemaDocuments.Add(new SchemaDocumentInfo(document));
        return builder;
    }

    /// <summary>
    /// Adds a GraphQL schema document to the schema builder by loading a document from a delegate.
    /// </summary>
    /// <param name="builder">
    /// The schema builder instance.
    /// </param>
    /// <param name="loadDocument">
    /// A delegate that loads the GraphQL schema document.
    /// </param>
    /// <returns>
    /// The schema builder instance with the document added.
    /// </returns>
    public static ISchemaBuilder AddDocument(
        this ISchemaBuilder builder,
        Func<IServiceProvider, DocumentNode> loadDocument)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loadDocument);

        var feature = builder.Features.GetOrSet<TypeSystemFeature>();
        feature.SchemaDocuments.Add(new SchemaDocumentInfo(loadDocument));
        return builder;
    }

    private static DocumentNode ParseDocument(string filePath)
    {
        Span<byte> buffer = stackalloc byte[256];
        byte[]? rentedBuffer = null;

        try
        {
            var totalBytesRead = 0;
            using var fileStream = File.OpenRead(filePath);

            while (true)
            {
                var sliced = buffer[totalBytesRead..];
                var bytesRead = fileStream.Read(sliced);
                totalBytesRead += bytesRead;

                if (bytesRead < sliced.Length)
                {
                    break;
                }

                var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                buffer.CopyTo(newBuffer);

                if (rentedBuffer is not null)
                {
                    buffer[..totalBytesRead].Clear();
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }

                buffer = newBuffer;
                rentedBuffer = newBuffer;
            }

            return Utf8GraphQLParser.Parse(buffer[..totalBytesRead]);
        }
        finally
        {
            if (rentedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }
}
