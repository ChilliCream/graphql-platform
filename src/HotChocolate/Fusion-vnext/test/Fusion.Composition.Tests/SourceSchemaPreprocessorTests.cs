using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaPreprocessorTests
{
    [Fact]
    public void Preprocess_ApplyInferredKeyDirectivesEnabled_AppliesInferredKeyDirectives()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    personById(id: ID!): Person @lookup
                    personByAddressId(id: ID! @is(field: "address.id")): Person @lookup
                    productById(id: ID!): Product @lookup
                    productByIdAgain(id: ID!): Product @lookup
                    productByIdAndCategoryId(id: ID!, categoryId: Int): Product @lookup
                    productByIdOrCategoryId(
                        idOrCategoryId: IdOrCategoryIdInput! @is(field: "{ id } | { categoryId }")
                    ): Product @lookup
                    petById(id: ID!): Pet @lookup # interface
                    fruitById(id: ID!): Fruit @lookup # union
                }

                type Person @key(fields: "id") { # Existing key should not be duplicated.
                    id: ID!
                    address: Address!
                }

                type Address {
                    id: ID!
                }

                type Product {
                    id: ID!
                    categoryId: Int
                }

                input IdOrCategoryIdInput @oneOf {
                    id: ID
                    categoryId: Int
                }

                interface Pet {
                    id: ID!
                }

                type Dog implements Pet {
                    id: ID!
                }

                type Cat implements Pet {
                    id: ID!
                }

                union Fruit = Apple | Orange

                type Apple {
                    id: ID!
                }

                type Orange {
                    id: ID!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var preprocessor = new SourceSchemaPreprocessor(schema, []);

        // act
        preprocessor.Process();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void Preprocess_ApplyInferredKeyDirectivesDisabled_DoesNotApplyInferredKeyDirectives()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    personById(id: ID!): Person @lookup
                }

                type Person {
                    id: ID!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                new SourceSchemaPreprocessorOptions { ApplyInferredKeyDirectives = false });

        // act
        preprocessor.Process();

        // assert
        Assert.False(schema.Types["Person"].Directives.ContainsName(WellKnownDirectiveNames.Key));
    }

    [Fact]
    public void Preprocess_InheritInterfaceKeysEnabled_InheritsInterfaceKeys()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                interface Animal @key(fields: "id") @key(fields: "age") {
                    id: ID!
                    age: Int
                }

                interface Pet implements Animal @key(fields: "name") {
                    id: ID!
                    age: Int
                    name: String
                }

                type Dog implements Pet & Animal {
                    id: ID!
                    age: Int
                    name: String
                }

                type Cat implements Pet & Animal {
                    id: ID!
                    age: Int
                    name: String
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var preprocessor = new SourceSchemaPreprocessor(schema, []);

        // act
        preprocessor.Process();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.ToString().MatchInlineSnapshot(
            // lang=graphql
            """
            type Cat implements Pet & Animal
                @key(fields: "name")
                @key(fields: "id")
                @key(fields: "age") {
                age: Int
                id: ID!
                name: String
            }

            type Dog implements Pet & Animal
                @key(fields: "name")
                @key(fields: "id")
                @key(fields: "age") {
                age: Int
                id: ID!
                name: String
            }

            interface Animal
                @key(fields: "id")
                @key(fields: "age") {
                age: Int
                id: ID!
            }

            interface Pet implements Animal
                @key(fields: "name")
                @key(fields: "id")
                @key(fields: "age") {
                age: Int
                id: ID!
                name: String
            }
            """);
    }

    [Fact]
    public void Preprocess_InheritInterfaceKeysDisabled_DoesNotInheritInterfaceKeys()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                interface Pet @key(fields: "id") {
                    id: ID!
                }

                type Cat implements Pet {
                    id: ID!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                new SourceSchemaPreprocessorOptions { InheritInterfaceKeys = false });

        // act
        preprocessor.Process();

        // assert
        Assert.False(schema.Types["Cat"].Directives.ContainsName(WellKnownDirectiveNames.Key));
    }

    [Fact]
    public void FusionV1CompatibilityMode_Should_Infer_Lookups()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                  node(id: ID!): Node
                  productById(id: ID!): Product
                  productByName(productName: String!): Product
                }

                interface Node {
                  id: ID!
                }

                type Product implements Node {
                  id: ID!
                  name: String!
                }

                type Review implements Node {
                  id: ID!
                  title: String!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                new SourceSchemaPreprocessorOptions { Version = new Version(1, 0, 0) });

        // act
        preprocessor.Process();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.ToString().MatchInlineSnapshot(
            // lang=graphql
            """
            schema {
              query: Query
            }

            type Query {
              node(id: ID!): Node
                @lookup
              productById(id: ID!): Product
                @lookup
              productByName(productName: String!
                @is(field: "name")): Product
                @lookup
            }

            type Product implements Node
              @key(fields: "id")
              @key(fields: "name") {
              id: ID!
              name: String!
            }

            type Review implements Node
              @key(fields: "id") {
              id: ID!
              title: String!
            }

            interface Node
              @key(fields: "id") {
              id: ID!
            }
            """);
    }

    [Fact]
    public void FusionV1CompatibilityMode_Should_Not_Infer_Lookups()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                  # gtin does not exist as a field on Product
                  productByGtin(gtin: String!): Product
                  # non-null return type
                  productById(id: ID!): Product!
                  # multiple arguments
                  productByIdAndOther(id: ID!, other: String): Product
                  # list return type
                  productsById(ids: [ID!]!): [Product]
                  # does not follow typeNameByFieldName convention
                  product(id: ID!): Product
                }

                type Product {
                  id: ID!
                  name: String!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                new SourceSchemaPreprocessorOptions { Version = new Version(1, 0, 0) });

        // act
        preprocessor.Process();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.ToString().MatchInlineSnapshot(
            // lang=graphql
            """
            schema {
              query: Query
            }

            type Query {
              product(id: ID!): Product
              productByGtin(gtin: String!): Product
              productById(id: ID!): Product!
              productByIdAndOther(id: ID! other: String): Product
              productsById(ids: [ID!]!): [Product]
            }

            type Product {
              id: ID!
              name: String!
            }
            """);
    }

    [Fact]
    public void FusionV1CompatibilityMode_Should_Strip_Batching_Fields()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                  productsById(ids: [ID!]! @is(field: "id")): [Product] @lookup
                  reviewsById(ids: [ID!]! @is(field: "id")): [Review] @lookup @internal
                }

                type Product {
                  id: ID!
                  name: String!
                }

                type Review {
                  id: ID!
                  name: String!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                new SourceSchemaPreprocessorOptions { Version = new Version(1, 0, 0) });

        // act
        preprocessor.Process();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.ToString().MatchInlineSnapshot(
            // lang=graphql
            """
            schema {
              query: Query
            }

            type Query {
              productsById(ids: [ID!]!): [Product]
            }

            type Product {
              id: ID!
              name: String!
            }

            type Review {
              id: ID!
              name: String!
            }
            """);
    }

    [Fact]
    public void FusionV1CompatibilityMode_Should_Apply_Shareable()
    {
        // arrange
        var sourceSchemaTextA =
            new SourceSchemaText(
                "A",
                """
                type Query {
                  productById(id: ID!): Product @lookup
                }

                type Product {
                  id: ID!
                  name: String!
                }
                """);

        var sourceSchemaTextB =
            new SourceSchemaText(
                "B",
                """
                type Query {
                  productById(id: ID!): Product @lookup
                }

                type Product {
                  id: ID!
                  name: String!
                  price: Float!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaTextA, sourceSchemaTextB], new CompositionLog());
        var schemas = sourceSchemaParser.Parse().Value;
        var schema = schemas.First();
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                schemas,
                new SourceSchemaPreprocessorOptions { Version = new Version(1, 0, 0) });

        // act
        preprocessor.Process();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.ToString().MatchInlineSnapshot(
            // lang=graphql
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product
                @lookup
                @shareable
            }

            type Product
              @key(fields: "id") {
              id: ID!
              name: String!
                @shareable
            }
            """);
    }
}
