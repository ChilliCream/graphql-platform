using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Validators;

public sealed class SelectionSetValidatorTests
{
    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string schemaText, string fieldCoordinateText)
    {
        // arrange
        var schema = SchemaParser.Parse(schemaText);
        var fieldCoordinate = SchemaCoordinate.Parse(fieldCoordinateText);

        if (!schema.TryGetMember(fieldCoordinate, out var member)
            || member is not MutableOutputFieldDefinition field)
        {
            Assert.Fail();
            return;
        }

        var providesDirective = field.Directives["provides"].First();
        var fieldsArgument = ((StringValueNode)providesDirective.Arguments["fields"]).Value;
        var selectionSet = ParseSelectionSet($"{{ {fieldsArgument} }}");
        var type = field.Type.AsTypeDefinition();

        // act
        var errors = new SelectionSetValidator(schema).Validate(selectionSet, type);

        // assert
        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(
        string schemaText,
        string fieldCoordinateText,
        string[] errorMessages)
    {
        // arrange
        var schema = SchemaParser.Parse(schemaText);
        var fieldCoordinate = SchemaCoordinate.Parse(fieldCoordinateText);

        if (!schema.TryGetMember(fieldCoordinate, out var member)
            || member is not MutableOutputFieldDefinition field)
        {
            Assert.Fail();
            return;
        }

        var providesDirective = field.Directives["provides"].First();
        var fieldsArgument = ((StringValueNode)providesDirective.Arguments["fields"]).Value;
        var selectionSet = ParseSelectionSet($"{{ {fieldsArgument} }}");
        var type = field.Type.AsTypeDefinition();

        // act
        var errors = new SelectionSetValidator(schema).Validate(selectionSet, type);

        // assert
        Assert.Equal(errorMessages, errors);
    }

    public static TheoryData<string, string> ValidExamplesData()
    {
        return new TheoryData<string, string>
        {
            // From https://graphql.github.io/composite-schemas-spec/draft/#sec--provides.
            {
                """
                type Review {
                    id: ID!
                    body: String!
                    author: User @provides(fields: "email")
                }

                type User @key(fields: "id") {
                    id: ID!
                    email: String! @external
                    name: String!
                }

                type Query {
                    reviews: [Review!]
                    users: [User!]
                }
                """,
                "Review.author"
            },
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "sku variation { size }")
                }

                type Product @key(fields: "sku variation { id }") {
                    sku: String! @external
                    variation: ProductVariation!
                    name: String!
                }

                type ProductVariation {
                    id: String!
                    size: String! @external
                }
                """,
                "Review.product"
            },
            {
                """"
                type Review {
                    id: ID!
                    # The @provides directive tells us that this source schema can supply different
                    # fields depending on which concrete type of Product is returned.
                    product: Product
                        @provides(
                            fields: """
                            ... on Book { author }
                            ... on Clothing { size }
                            """
                        )
                }

                interface Product @key(fields: "id") {
                    id: ID!
                }

                type Book implements Product {
                    id: ID!
                    title: String!
                    author: String! @external
                }

                type Clothing implements Product {
                    id: ID!
                    name: String!
                    size: String! @external
                }

                type Query {
                    reviews: [Review!]!
                }
                """",
                "Review.product"
            },
            // As above, but using a union instead of an interface.
            {
                """"
                type Review {
                    id: ID!
                    # The @provides directive tells us that this source schema can supply different
                    # fields depending on which concrete type of Product is returned.
                    product: Product
                        @provides(
                            fields: """
                            ... on Book { author }
                            ... on Clothing { size }
                            """
                        )
                }

                union Product = Book | Clothing

                type Book {
                    id: ID!
                    title: String!
                    author: String! @external
                }

                type Clothing {
                    id: ID!
                    name: String!
                    size: String! @external
                }

                type Query {
                    reviews: [Review!]!
                }
                """",
                "Review.product"
            }
        };
    }

    public static TheoryData<string, string, string[]> InvalidExamplesData()
    {
        return new TheoryData<string, string, string[]>
        {
            // Referencing an unknown field.
            {
                """
                type Review {
                    id: ID!
                    body: String!
                    author: User @provides(fields: "unknownField")
                }

                type User @key(fields: "id") {
                    id: ID!
                    email: String! @external
                    name: String!
                }

                type Query {
                    reviews: [Review!]
                    users: [User!]
                }
                """,
                "Review.author",
                ["The field 'unknownField' does not exist on the type 'User'."]
            },
            // Referencing an unknown field on a nested type.
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "sku variation { unknownField }")
                }

                type Product @key(fields: "sku variation { id }") {
                    sku: String! @external
                    variation: ProductVariation!
                    name: String!
                }

                type ProductVariation {
                    id: String!
                    size: String! @external
                }
                """,
                "Review.product",
                ["The field 'unknownField' does not exist on the type 'ProductVariation'."]
            },
            // Type condition referencing a missing type.
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "... on Book { author }")
                }

                interface Product @key(fields: "id") {
                    id: ID!
                }
                """,
                "Review.product",
                [
                    "The type condition in the selection set is invalid. Type 'Book' does not "
                    + "exist."
                ]
            },
            // Type condition referencing a type that does not implement the interface.
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "... on Book { author }")
                }

                interface Product @key(fields: "id") {
                    id: ID!
                }

                type Book {
                    id: ID!
                    author: String! @external
                }
                """,
                "Review.product",
                [
                    "The type 'Book' is not a possible type of type 'Product'."
                ]
            },
            // Type condition referencing a type that is not a union member.
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "... on Book { author }")
                }

                union Product = Clothing

                type Book {
                    id: ID!
                    author: String! @external
                }

                type Clothing {
                    id: ID!
                    name: String!
                }
                """,
                "Review.product",
                [
                    "The type 'Book' is not a possible type of type 'Product'."
                ]
            },
            // Empty selection set on field returning object type.
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "sku variation")
                }

                type Product @key(fields: "sku variation { id }") {
                    sku: String! @external
                    variation: ProductVariation!
                    name: String!
                }

                type ProductVariation {
                    id: String!
                    size: String! @external
                }
                """,
                "Review.product",
                [
                    "The field 'variation' returns a composite type and must have subselections."
                ]
            },
            // Empty selection set on field returning union type.
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "category")
                }

                type Product {
                    id: ID!
                    name: String!
                    category: Category!
                }

                union Category = Books | Clothing

                type Books {
                    id: ID!
                    name: String!
                }

                type Clothing {
                    id: ID!
                    name: String!
                }
                """,
                "Review.product",
                [
                    "The field 'category' returns a composite type and must have subselections."
                ]
            },
            // Invalid selection set on field returning union type.
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "category { name }")
                }

                type Product {
                    id: ID!
                    name: String!
                    category: Category!
                }

                union Category = Books | Clothing

                type Books {
                    id: ID!
                    name: String!
                }

                type Clothing {
                    id: ID!
                    name: String!
                }
                """,
                "Review.product",
                [
                    "The field 'category' returns a union type and must only include inline "
                    + "fragment selections."
                ]
            },
            // Selection on field returning scalar type.
            {
                """
                type Review {
                    id: ID!
                    product: Product @provides(fields: "id { name }")
                }

                type Product {
                    id: ID!
                    name: String!
                }
                """,
                "Review.product",
                [
                    "The field 'id' does not return a composite type and cannot have subselections."
                ]
            }
        };
    }
}
