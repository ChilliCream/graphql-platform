using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Logging;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using StringValueNode = HotChocolate.Language.StringValueNode;

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

    [Fact]
    public void InterfaceTypeCondition_WithPossibleTypeOverlap_IsValid()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    foo(input: RequirerInput @is(field: "{ foo: foo, bar: <Bar>.bar }")): Foo
                }

                interface Foo {
                    foo: String
                }

                interface Bar {
                    foo: String
                    bar: String
                }

                type Baz implements Foo & Bar {
                    foo: String
                    bar: String
                }

                input RequirerInput {
                    foo: String
                    bar: String
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value;
        var fieldSelectionMap = GetFieldSelectionMap(schema, "Query", "foo", "input", "is");
        var selectedValue = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputType = schema.QueryType!.Fields["foo"].Arguments["input"].Type;
        var outputType = schema.Types["Foo"];

        // act
        var errors =
            new FieldSelectionMapValidator(schema).Validate(selectedValue, inputType, outputType);

        // assert
        Assert.Empty(errors);
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
            { "[[BookIdAndTitleInput]]", "Query", "nestedBookList[[{ id, title }]]" },
            // A constant argument with a valid enum value is accepted.
            { "Float", "Book", "weight(unit: POUND)" }
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
            },
            // Unknown argument on a selected field.
            {
                "Float",
                "Book",
                "weight(scale: POUND)",
                ["The argument 'scale' does not exist on field 'Book.weight'."]
            },
            // Incompatible argument value.
            {
                "Float",
                "Book",
                "weight(unit: 5)",
                [
                    "The value provided for argument 'unit' on field 'Book.weight' is not compatible with the type 'WeightUnit'."
                ]
            }
        };
    }

    [Fact]
    public void ContextPath_Should_BeValid_When_TheContextTypeHasTheField()
    {
        // arrange
        var contextTypes = ContextTypes(("userContext", ["User"]));

        // act
        var errors = ValidateContextPath("$userContext.locale", "String", contextTypes);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ContextPath_Should_ReportTheFieldError_When_OneContextTypeLacksTheField()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));

        // act
        var errors = ValidateContextPath("$account.locale", "String", contextTypes);

        // assert
        Assert.Equal(["The field 'locale' does not exist on the type 'Organization'."], errors);
    }

    [Fact]
    public void ContextPath_Should_ReportTheErrorsOfEveryContextType_When_NoTypeHasTheField()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));

        // act
        var errors = ValidateContextPath("$account.missing", "String", contextTypes);

        // assert
        Assert.Equal(
            [
                "The field 'missing' does not exist on the type 'User'.",
                "The field 'missing' does not exist on the type 'Organization'."
            ],
            errors);
    }

    [Fact]
    public void ContextPath_Should_BeValid_When_EveryBranchHasATypeCondition()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));

        // act
        var errors =
            ValidateContextPath(
                "$account<User>.locale | $account<Organization>.defaultLocale",
                "String",
                contextTypes);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ContextPath_Should_ReportTheFieldError_When_TheTypeConditionTypeLacksTheField()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));

        // act
        var errors = ValidateContextPath("$account<Organization>.locale", "String", contextTypes);

        // assert
        Assert.Equal(["The field 'locale' does not exist on the type 'Organization'."], errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_TheTypeConditionTypeDoesNotSetTheContext()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));

        // act
        var errors = ValidateContextPath("$account<Invoice>.locale", "String", contextTypes);

        // assert
        Assert.Equal(["The type 'Invoice' does not set the context 'account'."], errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_TheContextIsNotSetByAnyType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", []));

        // act
        var errors = ValidateContextPath("$account.locale", "String", contextTypes);

        // assert
        Assert.Equal(["The context 'account' is not set by any type."], errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_TheContextIsUnknown()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User"]));

        // act
        var errors = ValidateContextPath("$other<User>.locale", "String", contextTypes);

        // assert
        Assert.Equal(["The context 'other' is not set by any type."], errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_TheContextHasNoTypesInTheOverloadWithoutContextTypes()
    {
        // arrange
        var selection = new FieldSelectionMapParser("$account.locale").Parse();
        var inputType = s_contextSchema.Types["String"];

        // act
        var errors =
            new FieldSelectionMapValidator(s_contextSchema)
                .Validate(selection, inputType, s_contextSchema.QueryType!);

        // assert
        Assert.Equal(["The context 'account' is not set by any type."], errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_TheTypeConditionTypeDoesNotExist()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User"]));

        // act
        var errors = ValidateContextPath("$account<Missing>.locale", "String", contextTypes);

        // assert
        Assert.Equal(
            ["The type condition in path '$account<Missing>.locale' is invalid. Type 'Missing' does not exist."],
            errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_TheTypeConditionTypeIsNotAnObjectType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["Node"]));

        // act
        var errors = ValidateContextPath("$account<Node>.id", "ID", contextTypes);

        // assert
        Assert.Equal(
            ["The type condition 'Node' on context 'account' must be an object type."],
            errors);
    }

    [Fact]
    public void ContextPath_Should_BeValid_When_TheTypeConditionTypeImplementsAnInterfaceContextType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["Node"]));

        // act
        var errors = ValidateContextPath("$account<User>.locale", "String", contextTypes);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ContextPath_Should_BeValid_When_TheTypeConditionTypeIsAMemberOfAUnionContextType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["Owner"]));

        // act
        var errors = ValidateContextPath("$account<Organization>.defaultLocale", "String", contextTypes);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_TheTypeConditionTypeIsNotAMemberOfAUnionContextType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["Owner"]));

        // act
        var errors = ValidateContextPath("$account<Invoice>.id", "ID", contextTypes);

        // assert
        Assert.Equal(["The type 'Invoice' does not set the context 'account'."], errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_TheLeafTypeIsNotCompatibleWithTheInputType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User"]));

        // act
        var errors = ValidateContextPath("$account.locale", "ID", contextTypes);

        // assert
        Assert.Equal(
            ["The field 'locale' is of type 'String!' instead of the expected input type 'ID'."],
            errors);
    }

    [Fact]
    public void ContextPath_Should_ReportAnError_When_ANullableFieldIsOnThePathToANonNullInputType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User"]));

        // act
        var errors =
            ValidateContextPath(
                "$account.address.city",
                "String!",
                contextTypes,
                disallowNullableFieldsOnPathToNonNullInputType: true);

        // assert
        Assert.Equal(
            ["The input type 'String!' is non-null, but one of the fields on the path 'address' returns a nullable type."],
            errors);
    }

    [Fact]
    public void ContextPath_Should_ValidateTheObjectSelectionForEveryContextType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));

        // act
        var errors =
            ValidateContextPath("$account.address.{ city: city }", "AddressInput", contextTypes);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_When_TypedContextPathHasAnObjectSelection()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User"]));

        // act
        var errors =
            ValidateContextPath("$account<User>.address.{ city }", "AddressInput", contextTypes);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_When_TypedContextPathHasAListSelection()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User"]));

        // act
        var errors =
            ValidateContextPath("$account<User>.tags[{ name }]", "[TagInput!]!", contextTypes);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ContextPath_Should_ReportTheListSelectionErrors_When_OneContextTypeLacksTheField()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));

        // act
        var errors =
            ValidateContextPath("$account.tags[name]", "[String!]!", contextTypes);

        // assert
        Assert.Equal(["The field 'tags' does not exist on the type 'Organization'."], errors);
    }

    [Fact]
    public void ContextPath_Should_BeValid_When_ItIsNestedInAnObjectSelection()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User"]));

        // act
        var errors =
            ValidateContextPath(
                "{ id: $account.id, locale: $account.locale }",
                "AccountInput",
                contextTypes);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void ContextPath_Should_ReportOneErrorPerBranch_When_ItIsNestedInAnObjectSelection()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));

        // act
        var errors =
            ValidateContextPath(
                "{ id: $account.id, locale: $account.locale }",
                "AccountInput",
                contextTypes);

        // assert
        Assert.Equal(["The field 'locale' does not exist on the type 'Organization'."], errors);
    }

    [Fact]
    public void ContextPath_Should_ReturnTheSelectedFieldsOfEveryContextType()
    {
        // arrange
        var contextTypes = ContextTypes(("account", ["User", "Organization"]));
        var selection =
            new FieldSelectionMapParser("$account.id").Parse();
        var inputType = s_contextSchema.Types["ID"];

        // act
        var errors =
            new FieldSelectionMapValidator(s_contextSchema)
                .Validate(
                    selection,
                    inputType,
                    s_contextSchema.QueryType!,
                    contextTypes,
                    out var selectedFields);

        // assert
        Assert.Empty(errors);
        Assert.Equal(
            ["Organization.id", "User.id"],
            selectedFields.Select(f => f.Coordinate.ToString()).Order());
    }

    private static ImmutableArray<string> ValidateContextPath(
        string fieldSelectionMap,
        string inputTypeReference,
        IReadOnlyDictionary<string, ImmutableArray<ITypeDefinition>> contextTypes,
        bool disallowNullableFieldsOnPathToNonNullInputType = false)
    {
        var selection = new FieldSelectionMapParser(fieldSelectionMap).Parse();
        var inputTypeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(inputTypeReference);
        var inputTypeDefinition = s_contextSchema.Types[inputTypeNode.NamedType().Name.Value];
        var inputType = inputTypeNode.RewriteToType(inputTypeDefinition);

        return new FieldSelectionMapValidator(
                s_contextSchema,
                disallowNullableFieldsOnPathToNonNullInputType)
            .Validate(selection, inputType, s_contextSchema.QueryType!, contextTypes);
    }

    private static Dictionary<string, ImmutableArray<ITypeDefinition>> ContextTypes(
        params (string ContextName, string[] TypeNames)[] contexts)
    {
        var contextTypes = new Dictionary<string, ImmutableArray<ITypeDefinition>>();

        foreach (var (contextName, typeNames) in contexts)
        {
            contextTypes[contextName] =
                [.. typeNames.Select(n => s_contextSchema.Types[n])];
        }

        return contextTypes;
    }

    private static readonly MutableSchemaDefinition s_contextSchema =
        SchemaParser.Parse(
            """
            type Query {
                user: User
            }

            interface Node {
                id: ID!
            }

            type User implements Node {
                id: ID!
                locale: String!
                address: Address
                tags: [Tag!]!
            }

            type Organization implements Node {
                id: ID!
                defaultLocale: String!
                address: Address
            }

            type Invoice {
                id: ID!
            }

            union Owner = User | Organization

            type Address {
                city: String!
            }

            type Tag {
                name: String!
            }

            input AddressInput {
                city: String!
            }

            input TagInput {
                name: String!
            }

            input AccountInput {
                id: ID!
                locale: String!
            }
            """);

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
                weight(unit: WeightUnit = KILOGRAM): Float # Added
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

            # Added
            enum WeightUnit { KILOGRAM POUND }
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
