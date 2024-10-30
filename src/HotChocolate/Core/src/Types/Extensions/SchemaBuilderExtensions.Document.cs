using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate;

public static partial class SchemaBuilderExtensions
{
    public static ISchemaBuilder AddDocumentFromString(
        this ISchemaBuilder builder,
        [StringSyntax("graphql")] string schema)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(schema))
        {
            throw new ArgumentException(
                TypeResources.SchemaBuilderExtensions_SchemaIsEmpty,
                nameof(schema));
        }

        return builder.AddDocument(sp => Utf8GraphQLParser.Parse(schema));
    }

    public static ISchemaBuilder AddDocumentFromFile(
        this ISchemaBuilder builder,
        string filePath)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException(
                "",
                nameof(filePath));
        }

        return builder.AddDocument(_ => Utf8GraphQLParser.Parse(File.ReadAllBytes(filePath)));
    }

    public static ISchemaBuilder AddDocument(
        this ISchemaBuilder builder,
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

        return builder.AddDocument(_ => document);
    }
}
