using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace ApolloErrorVariablesMicro;

internal static class SchemaFixture
{
    internal static DocumentNode ComposeApolloSchema()
    {
        const string sourceSchemaA =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@listSize"]) {
              query: Query
            }
            type Product @key(fields: "id") { id: ID! }
            type Category @key(fields: "id") { id: ID! }
            type Query {
              products: [Product] @listSize(assumedSize: 96)
              categories: [Category] @listSize(assumedSize: 96)
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }
            type _Service { sdl: String! }
            union _Entity = Product | Category
            scalar FieldSet
            scalar _Any
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @listSize(assumedSize: Int slicingArguments: [String!] sizedFields: [String!] requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
            """;
        const string sourceSchemaB =
            """
            schema @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key", "@listSize"]) {
              query: Query
            }
            type Product @key(fields: "id") { id: ID! name: String }
            type Category @key(fields: "id") { id: ID! title: String }
            type Query {
              _service: _Service!
              _entities(representations: [_Any!]!): [_Entity]!
            }
            type _Service { sdl: String! }
            union _Entity = Product | Category
            scalar FieldSet
            scalar _Any
            directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            directive @listSize(assumedSize: Int slicingArguments: [String!] sizedFields: [String!] requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION
            """;
        var sources = new[]
        {
            new SourceSchemaText("a", sourceSchemaA),
            new SourceSchemaText("b", sourceSchemaB)
        };
        var options = new SchemaComposerOptions();

        foreach (var source in sources)
        {
            options.SourceSchemas[source.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions { InferKeysFromLookups = false }
            };
        }

        return new SchemaComposer(sources, options, new CompositionLog()).Compose().Value.ToSyntaxNode();
    }

}
