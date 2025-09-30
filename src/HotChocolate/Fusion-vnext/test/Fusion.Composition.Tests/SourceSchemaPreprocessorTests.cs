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
        var preprocessor = new SourceSchemaPreprocessor(schema);

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

                type Dog implements Pet {
                    id: ID!
                    age: Int
                    name: String
                }

                type Cat implements Pet {
                    id: ID!
                    age: Int
                    name: String
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var preprocessor = new SourceSchemaPreprocessor(schema);

        // act
        preprocessor.Process();
        schema.Types.Remove("FieldSelectionMap");
        schema.Types.Remove("FieldSelectionSet");
        schema.DirectiveDefinitions.Clear();

        // assert
        schema.ToString().MatchInlineSnapshot(
            // lang=graphql
            """
            type Cat implements Pet
                @key(fields: "name")
                @key(fields: "id")
                @key(fields: "age") {
                age: Int
                id: ID!
                name: String
            }

            type Dog implements Pet
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
                new SourceSchemaPreprocessorOptions { InheritInterfaceKeys = false });

        // act
        preprocessor.Process();

        // assert
        Assert.False(schema.Types["Cat"].Directives.ContainsName(WellKnownDirectiveNames.Key));
    }
}
