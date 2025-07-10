using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Validators;

public sealed class FieldSelectionMapValidatorTests
{
    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(
        string inputTypeName,
        string outputTypeName,
        string fieldSelectionMap)
    {
        // arrange
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = s_schema1.Types[inputTypeName];
        var outputType = s_schema1.Types[outputTypeName];

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
        string inputTypeName,
        string outputTypeName,
        string fieldSelectionMap,
        string[] errorMessages)
    {
        // arrange
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = s_schema1.Types[inputTypeName];
        var outputType = s_schema1.Types[outputTypeName];

        // act
        var errors =
            new FieldSelectionMapValidator(s_schema1).Validate(
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
        var schema = SchemaParser.Parse(
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
        const string fieldSelectionMap = "mediaById.id";
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.Types["ID"];
        var outputType = schema.Types["Query"];

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
        var schema = SchemaParser.Parse(
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
        var fieldSelectionMap = GetFieldSelectionMap(schema, "Query", "userById", "user", "is");
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.Types["UserInput"];
        var outputType = schema.Types["User"];

        // act
        var errors =
            new FieldSelectionMapValidator(schema).Validate(selectedValue, inputType, outputType);

        // assert
        string[] expected =
            ["The selection on input type 'UserInput' must include all required fields."];
        Assert.Equal(expected, errors);
    }

    // If the "UserInput" type requires the "name" field, but the "User" type has an optional "name"
    // field, the following selection would be valid.
    [Fact]
    public void RequiredSelectedObjectFields_Example2()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            type Query {
                findUser(input: UserInput! @is(field: "{ name: name }")): User! @lookup
            }

            type User {
                id: ID
                name: String
            }

            input UserInput {
                id: ID
                name: String!
            }
            """);
        var fieldSelectionMap = GetFieldSelectionMap(schema, "Query", "findUser", "input", "is");
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.Types["UserInput"];
        var outputType = schema.Types["User"];

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
        var schema = SchemaParser.Parse(
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
        var fieldSelectionMap = GetFieldSelectionMap(schema, "Query", "findUser", "input", "is");
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.Types["UserInput"];
        var outputType = schema.Types["User"];

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
        var schema = SchemaParser.Parse(
            $$"""
            type Query {
                person(
                    by: PersonByInput @is(field: "{{fieldSelectionMap}}")
                ): Person
            }

            type Person {
                id: ID
                name: String
                address: Address
            }

            type Address {
                id: ID
            }

            input PersonByInput @oneOf {
                id: ID
                addressId: ID
                name: String
            }
            """);
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.Types["PersonByInput"];
        var outputType = schema.Types["Person"];

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
            { "String", "Book", "{ id, title }" },
            { "ID", "Query", "mediaById<Book>.author.id | mediaById<Movie>.id" },
            { "ID", "Media", "{ bookId: <Book>.author.id } | { movieId: <Movie>.id }" }
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
                    "The field 'title' does not return a composite type and cannot have " +
                    "subselections."
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
                ["The field 'id' is of type 'ID' instead of the expected input type 'String'."]
            },
            // TODO: 6.3.5 Selected Object Field Names examples.
            // TODO: 6.3.6 Selected Object Field Uniqueness examples.
            // Blocked by https://github.com/graphql/composite-schemas-spec/issues/171.
            // Additional tests.
            {
                "String",
                "Book",
                "{ id, unknownField1, unknownField2 }",
                [
                    "The field 'unknownField1' does not exist on the type 'Book'.",
                    "The field 'unknownField2' does not exist on the type 'Book'."
                ]
            },
            {
                "ID",
                "Media",
                "<MissingType>.movieId",
                [
                    "The type condition in path '<MissingType>.movieId' is invalid. Type " +
                    "'MissingType' does not exist."
                ]
            },
            {
                "ID",
                "Query",
                "mediaById<MissingType>.movieId",
                [
                    "The type condition in path 'mediaById<MissingType>.movieId' is invalid. " +
                    "Type 'MissingType' does not exist."
                ]
            },
            {
                "ID",
                "Query",
                "mediaById<Store>.id",
                [
                    "The type 'Store' is not a possible type of type 'Media'."
                ]
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
