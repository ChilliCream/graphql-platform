using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaPreprocessorTests
{
    [Fact]
    public void Preprocess_ExcludeByTag_RemovesTaggedMembers()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Object1 @tag(name: "remove") {
                    field1: ID!
                }

                type Object2 {
                    field1: ID!
                    field2: Int @tag(name: "remove")
                }

                type Object3 {
                    field1: ID!
                    field2(argument1: Int, argument2: Int @tag(name: "remove")): Int
                }

                interface Interface1 @tag(name: "remove") {
                    field1: ID!
                }

                interface Interface2 {
                    field1: ID!
                    field2: Int @tag(name: "remove")
                }

                union Union1 @tag(name: "remove") = Object1 | Object2

                input Input1 @tag(name: "remove") {
                    field1: ID!
                }

                input Input2 {
                    field1: ID!
                    field2: Int @tag(name: "remove")
                }

                enum Enum1 @tag(name: "remove") {
                    VALUE1
                }

                enum Enum2 {
                    VALUE1
                    VALUE2 @tag(name: "remove")
                }

                scalar Scalar1 @tag(name: "remove")

                directive @tag(name: String!) repeatable on
                    | SCHEMA
                    | SCALAR
                    | OBJECT
                    | FIELD_DEFINITION
                    | ARGUMENT_DEFINITION
                    | INTERFACE
                    | UNION
                    | ENUM
                    | ENUM_VALUE
                    | INPUT_OBJECT
                    | INPUT_FIELD_DEFINITION
                """);
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                compositionLog,
                options: new SourceSchemaPreprocessorOptions { ExcludeByTag = ["remove"] });

        // act
        preprocessor.Preprocess();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void Preprocess_ExcludeByTagInvalidSchema_ReturnsError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    field1: Object
                    field2(argument: Input): Int
                }

                type Object @tag(name: "remove") {
                    field: ID!
                }

                input Input @tag(name: "remove") {
                    field: ID!
                }

                directive @tag(name: String!) repeatable on
                    | SCHEMA
                    | SCALAR
                    | OBJECT
                    | FIELD_DEFINITION
                    | ARGUMENT_DEFINITION
                    | INTERFACE
                    | UNION
                    | ENUM
                    | ENUM_VALUE
                    | INPUT_OBJECT
                    | INPUT_FIELD_DEFINITION
                """);
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                compositionLog,
                options: new SourceSchemaPreprocessorOptions { ExcludeByTag = ["remove"] });

        // act
        var result = preprocessor.Preprocess();

        // assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Source schema preprocessing failed.", error.Message);
        Assert.Collection(
            compositionLog,
            logEntry =>
            {
                Assert.Equal("HCV0021", logEntry.Code);
                Assert.Equal(
                    "The type 'Object' of field 'Query.field1' is not defined in the schema. (Schema: 'A')",
                    logEntry.Message);
                Assert.Equal(LogSeverity.Error, logEntry.Severity);
            },
            logEntry =>
            {
                Assert.Equal("HCV0022", logEntry.Code);
                Assert.Equal(
                    "The type 'Input' of argument 'Query.field2(argument:)' is not defined in the schema. "
                    + "(Schema: 'A')",
                    logEntry.Message);
                Assert.Equal(LogSeverity.Error, logEntry.Severity);
            });
    }

    [Fact]
    public void Preprocess_InferKeysFromLookupsEnabled_AppliesInferredKeyDirectives()
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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor = new SourceSchemaPreprocessor(schema, [], compositionLog);

        // act
        preprocessor.Preprocess();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void Preprocess_InferKeysFromLookupsDisabled_DoesNotApplyInferredKeyDirectives()
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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                compositionLog,
                options: new SourceSchemaPreprocessorOptions { InferKeysFromLookups = false });

        // act
        preprocessor.Preprocess();

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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor = new SourceSchemaPreprocessor(schema, [], compositionLog);

        // act
        preprocessor.Preprocess();
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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                compositionLog,
                options: new SourceSchemaPreprocessorOptions { InheritInterfaceKeys = false });

        // act
        preprocessor.Preprocess();

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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                compositionLog,
                new Version(1, 0, 0));

        // act
        preprocessor.Preprocess();
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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                compositionLog,
                new Version(1, 0, 0));

        // act
        preprocessor.Preprocess();
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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                [],
                compositionLog,
                new Version(1, 0, 0));

        // act
        preprocessor.Preprocess();
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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser1 = new SourceSchemaParser(sourceSchemaTextA, compositionLog);
        var sourceSchemaParser2 = new SourceSchemaParser(sourceSchemaTextB, compositionLog);
        var schema1 = sourceSchemaParser1.Parse().Value;
        var schema2 = sourceSchemaParser2.Parse().Value;
        var schemas =
            ImmutableSortedSet.Create(
                new SchemaByNameComparer<MutableSchemaDefinition>(), schema1, schema2);
        var schema = schemas[0];
        var preprocessor =
            new SourceSchemaPreprocessor(
                schema,
                schemas,
                compositionLog,
                new Version(1, 0, 0));

        // act
        preprocessor.Preprocess();
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
