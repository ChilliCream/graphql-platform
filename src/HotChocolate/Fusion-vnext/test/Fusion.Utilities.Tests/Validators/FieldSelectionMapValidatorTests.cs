using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Logging;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Validators;

public sealed class FieldSelectionMapValidatorTests
{
    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(
        string inputTypeReference,
        string outputTypeReference,
        string fieldSelectionMap)
    {
        // arrange
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputTypeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(inputTypeReference);
        var inputTypeDefinition = s_schema1.Types[inputTypeNode.NamedType().Name.Value];
        var inputType = inputTypeNode.RewriteToType(inputTypeDefinition);
        var outputTypeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(outputTypeReference);
        var outputTypeDefinition = s_schema1.Types[outputTypeNode.NamedType().Name.Value];
        var outputType = outputTypeNode.RewriteToType(outputTypeDefinition);

        // act
        var errors =
            new FieldSelectionMapValidator(s_schema1).Validate(
                selectedValue,
                inputType,
                outputType);

        // assert
        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(
        string inputTypeReference,
        string outputTypeReference,
        string fieldSelectionMap,
        string[] errorMessages)
    {
        // arrange
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputTypeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(inputTypeReference);
        var inputTypeDefinition = s_schema1.Types[inputTypeNode.NamedType().Name.Value];
        var inputType = inputTypeNode.RewriteToType(inputTypeDefinition);
        var outputTypeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(outputTypeReference);
        var outputTypeDefinition = s_schema1.Types[outputTypeNode.NamedType().Name.Value];
        var outputType = outputTypeNode.RewriteToType(outputTypeDefinition);

        // act
        var errors =
            new FieldSelectionMapValidator(s_schema1, true).Validate(
                selectedValue,
                inputType,
                outputType);

        // assert
        Assert.Equal(errorMessages, errors);
    }

    [Fact]
    public void FieldReturningUnionTypeDoesNotSpecifyConcreteType()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    mediaById(id: ID!): Media!
                }

                union Media = Book | Movie

                type Book {
                    id: ID!
                }

                type Movie {
                    id: ID!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value;
        const string fieldSelectionMap = "mediaById.id";
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.QueryType!.Fields["mediaById"].Arguments["id"].Type;
        var outputType = schema.QueryType;

        // act
        var errors =
            new FieldSelectionMapValidator(schema).Validate(selectedValue, inputType, outputType);

        // assert
        string[] expected =
            ["The field 'mediaById' returns a union type and must have a type condition."];
        Assert.Equal(expected, errors);
    }

    // If the "UserInput" type requires the "id" field, then an invalid selection would be missing
    // the required "id" field.
    [Fact]
    public void RequiredSelectedObjectFields_Example1()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    userById(user: UserInput! @is(field: "{ name: name }")): User! @lookup
                }

                type User {
                    id: ID
                    name: String
                }

                input UserInput {
                    id: ID!
                    name: String!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value;
        var fieldSelectionMap = GetFieldSelectionMap(schema, "Query", "userById", "user", "is");
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.QueryType!.Fields["userById"].Arguments["user"].Type;
        var outputType = schema.QueryType!.Fields["userById"].Type;

        // act
        var errors =
            new FieldSelectionMapValidator(schema).Validate(selectedValue, inputType, outputType);

        // assert
        string[] expected =
            ["The selection on input type 'UserInput' must include all required fields."];
        Assert.Equal(expected, errors);
    }

    // If the "UserInput" type has an optional "name" field, but the "User" type requires the "name"
    // field, the following selection would be valid.
    [Fact]
    public void RequiredSelectedObjectFields_Example2()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    findUser(input: UserInput! @is(field: "{ name: name }")): User! @lookup
                }

                type User {
                    id: ID
                    name: String!
                }

                input UserInput {
                    id: ID
                    name: String
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value;
        var fieldSelectionMap = GetFieldSelectionMap(schema, "Query", "findUser", "input", "is");
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.QueryType!.Fields["findUser"].Arguments["input"].Type;
        var outputType = schema.QueryType!.Fields["findUser"].Type;

        // act
        var errors =
            new FieldSelectionMapValidator(schema).Validate(selectedValue, inputType, outputType);

        // assert
        Assert.Empty(errors);
    }

    // If the "UserInput" type requires the "name" field, but it's not defined in the "User" type,
    // the selection would be invalid.
    [Fact]
    public void RequiredSelectedObjectFields_Example3()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    findUser(input: UserInput! @is(field: "{ id: id }")): User! @lookup
                }

                type User {
                    id: ID
                }

                input UserInput {
                    id: ID
                    name: String!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value;
        var fieldSelectionMap = GetFieldSelectionMap(schema, "Query", "findUser", "input", "is");
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.QueryType!.Fields["findUser"].Arguments["input"].Type;
        var outputType = schema.QueryType!.Fields["findUser"].Type;

        // act
        var errors =
            new FieldSelectionMapValidator(schema).Validate(selectedValue, inputType, outputType);

        // assert
        string[] expected =
            ["The selection on input type 'UserInput' must include all required fields."];
        Assert.Equal(expected, errors);
    }

    [Theory]
    [InlineData(
        // From https://graphql.github.io/composite-schemas-spec/draft/#sec--is.
        "{ id } | { addressId: address.id } | { name }",
        new string[] { })]
    [InlineData(
        "{ }",
        new[]
        {
            "The selection on one-of input type 'PersonByInput' must include a single field."
        })]
    [InlineData(
        "{ id, name }",
        new[]
        {
            "The selection on one-of input type 'PersonByInput' must include a single field."
        })]
    public void RequiredSelectedObjectFields_Example4(string fieldSelectionMap, string[] expected)
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                $$"""
                type Query {
                    person(
                        by: PersonByInput @is(field: "{{fieldSelectionMap}}")
                    ): Person
                }

                type Person {
                    id: ID!
                    name: String
                    address: Address!
                }

                type Address {
                    id: ID!
                }

                input PersonByInput @oneOf {
                    id: ID
                    addressId: ID
                    name: String
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value;
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.QueryType!.Fields["person"].Arguments["by"].Type;
        var outputType = schema.QueryType!.Fields["person"].Type;

        // act
        var errors =
            new FieldSelectionMapValidator(schema).Validate(selectedValue, inputType, outputType);

        // assert
        Assert.Equal(expected, errors);
    }

    public static TheoryData<string, string, string> ValidExamplesData()
    {
        return new TheoryData<string, string, string>
        {
            // The following Path is valid in the context of Book.
            { "String", "Book", "title" },
            { "String", "Book", "<Book>.title" },
            // For non-leaf fields, the Path must continue to specify subselections until a leaf
            // field is reached.
            { "ID", "Book", "author.id" },
            // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedValue
            { "String", "Query", "mediaById<Book>.title | mediaById<Movie>.movieTitle" },
            { "FindMediaInput", "Media", "{ bookId: <Book>.id } | { movieId: <Movie>.id }" },
            { "Nested", "Media", "{ nested: { bookId: <Book>.id } | { movieId: <Movie>.id } }" },
            // Other tests.
            { "BookIdAndTitleInput", "Book", "{ id, title }" },
            { "IdInput", "Query", "storeById.{ id }" },
            { "IdInput", "Query", "storeById.{ id } | storeById.{ id }" },
            { "[ID]", "Query", "storeById.media[id]" },
            { "[ID]", "Query", "storeById.media[id] | storeById.media[id]" },
            { "[[ID]]", "Query", "nestedBookList[[id]]" },
            { "[[BookIdAndTitleInput]]", "Query", "nestedBookList[[{ id, title }]]" }
        };
    }

    public static TheoryData<string, string, string, string[]> InvalidExamplesData()
    {
        return new TheoryData<string, string, string, string[]>
        {
            // Incorrect paths where the field does not exist on the specified type are not valid
            // and result in validation errors.
            {
                "ID",
                "Book",
                "movieId",
                ["The field 'movieId' does not exist on the type 'Book'."]
            },
            {
                "ID",
                "Media",
                "<Book>.movieId",
                ["The field 'movieId' does not exist on the type 'Book'."]
            },
            // The following Path is invalid because "title" should not have subselections.
            {
                "String",
                "Book",
                "title.something",
                [
                    "The field 'title' does not return a composite type and cannot have "
                    + "subselections."
                ]
            },
            // Invalid Path where non-leaf fields do not have further selections.
            {
                "Author",
                "Book",
                "author",
                ["The field 'author' returns a composite type and must have subselections."]
            },
            // Non-coercible values are invalid. The following example is invalid.
            {
                "String",
                "Store",
                "id",
                ["The field 'id' is of type 'ID!' instead of the expected input type 'String'."]
            },
            {
                "ID!",
                "Book",
                "nullableAuthor.id",
                ["The input type 'ID!' is non-null, but one of the fields on the path 'nullableAuthor' returns a nullable type."]
            },
            // TODO: 6.3.5 Selected Object Field Names examples.
            // TODO: 6.3.6 Selected Object Field Uniqueness examples.
            // Blocked by https://github.com/graphql/composite-schemas-spec/issues/171.
            // Additional tests.
            {
                "BookIdAndTitleInput",
                "Book",
                "{ id, title, unknownField1, unknownField2 }",
                [
                    "The field 'unknownField1' does not exist on the input type 'BookIdAndTitleInput'.",
                    "The field 'unknownField2' does not exist on the input type 'BookIdAndTitleInput'."
                ]
            },
            {
                "ID",
                "Media",
                "<MissingType>.movieId",
                [
                    "The type condition in path '<MissingType>.movieId' is invalid. Type "
                    + "'MissingType' does not exist."
                ]
            },
            {
                "ID",
                "Query",
                "mediaById<MissingType>.movieId",
                [
                    "The type condition in path 'mediaById<MissingType>.movieId' is invalid. "
                    + "Type 'MissingType' does not exist."
                ]
            },
            {
                "ID",
                "Query",
                "mediaById<Store>.id",
                ["The type 'Store' is not a possible type of type 'Media'."]
            },
            {
                "ID",
                "Query",
                "storeById.{ id }",
                ["Expected an input object type but found 'ID'."]
            },
            {
                "ID",
                "Book",
                "{ id, title }",
                ["Expected an input object type but found 'ID'."]
            },
            {
                "[[[BookIdAndTitleInput]]]",
                "Query",
                "nestedBookList[[{ id, title }]]",
                ["Expected an input object type but found '[BookIdAndTitleInput]'."]
            },
            {
                "[[BookIdAndTitleInput]]",
                "Query",
                "nestedBookList[[{ id }]]",
                ["The selection on input type 'BookIdAndTitleInput' must include all required fields."]
            }
        };
    }

    private static readonly MutableSchemaDefinition s_schema1 =
        // From https://graphql.github.io/composite-schemas-spec/draft/#sec-Validation.
        SchemaParser.Parse(
            """
            type Query {
                mediaById(mediaId: ID!): Media
                findMedia(input: FindMediaInput): Media
                searchStore(search: SearchStoreInput): [Store]!
                storeById(id: ID!): Store
                nestedBookList: [[Book]] # Added
            }

            type Store {
                id: ID!
                city: String!
                media: [Media!]!
            }

            interface Media {
                id: ID!
            }

            type Book implements Media {
                id: ID!
                title: String!
                isbn: String!
                author: Author!
                nullableAuthor: Author # Added
            }

            type Movie implements Media {
                id: ID!
                movieTitle: String!
                releaseDate: String!
            }

            type Author {
                id: ID!
                books: [Book!]!
            }

            input FindMediaInput @oneOf {
                bookId: ID
                movieId: ID
            }

            input SearchStoreInput {
                city: String
                hasInStock: FindMediaInput
            }

            input Nested {
                nested: FindMediaInput
            }

            # Added
            input BookIdAndTitleInput {
                id: ID!
                title: String!
            }

            # Added
            input IdInput {
                id: ID!
            }
            """);

    private static ReadOnlySpan<char> GetFieldSelectionMap(
        MutableSchemaDefinition schema,
        string typeName,
        string fieldName,
        string argumentName,
        string directiveName)
    {
        var type = schema.Types[typeName];

        if (type is not MutableComplexTypeDefinition complexType)
        {
            throw new InvalidOperationException("Type must be a complex type.");
        }

        var field = complexType.Fields[fieldName];
        var argument = field.Arguments[argumentName];
        var directive = argument.Directives[directiveName].First();

        return ((StringValueNode)directive.Arguments["field"]).Value;
    }
}
