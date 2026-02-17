using HotChocolate.Language.Utilities;

namespace HotChocolate.Language.SyntaxTree;

public class SyntaxWriterTests
{
    [Fact]
    public void WriteMany_WithNewlineSeparator_WritesIndentation()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        writer.Indent();
        var items = new[] { "item1", "item2", "item3" };

        // act
        writer.WriteMany(
            items,
            (item, w) => w.Write(item),
            Environment.NewLine);

        var result = writer.ToString();

        // assert
        var expected = $"item1{Environment.NewLine}  item2{Environment.NewLine}  item3";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteMany_WithCommaSeparator_DoesNotWriteIndentation()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        writer.Indent();
        var items = new[] { "item1", "item2", "item3" };

        // act
        writer.WriteMany(
            items,
            (item, w) => w.Write(item),
            ", ");

        var result = writer.ToString();

        // assert
        Assert.Equal("item1, item2, item3", result);
    }

    [Fact]
    public void WriteMany_WithMultipleIndentLevels_WritesCorrectIndentation()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        writer.Indent();
        writer.Indent();
        var items = new[] { "a", "b", "c" };

        // act
        writer.WriteMany(
            items,
            (item, w) => w.Write(item),
            Environment.NewLine);

        var result = writer.ToString();

        // assert
        var expected = $"a{Environment.NewLine}    b{Environment.NewLine}    c";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteObjectValue_WithIndentation_FormatsCorrectly()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        var objectValue = new ObjectValueNode(
            new ObjectFieldNode("field1", "value1"),
            new ObjectFieldNode("field2", "value2"),
            new ObjectFieldNode("field3", "value3"));

        // act
        writer.WriteObjectValue(objectValue);
        var result = writer.ToString();

        // assert
        Assert.Equal("{ field1: \"value1\", field2: \"value2\", field3: \"value3\" }", result);
    }

    [Fact]
    public void WriteListValue_WithIndentation_FormatsCorrectly()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        var listValue = new ListValueNode(
            new IntValueNode(1),
            new IntValueNode(2),
            new IntValueNode(3));

        // act
        writer.WriteListValue(listValue);
        var result = writer.ToString();

        // assert
        Assert.Equal("[ 1, 2, 3 ]", result);
    }

    [Fact]
    public void WriteMany_WithEmptyList_WritesNothing()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        var items = Array.Empty<string>();

        // act
        writer.WriteMany(
            items,
            (item, w) => w.Write(item),
            ", ");

        var result = writer.ToString();

        // assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void WriteMany_WithSingleItem_DoesNotWriteSeparator()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        writer.Indent();
        var items = new[] { "single" };

        // act
        writer.WriteMany(
            items,
            (item, w) => w.Write(item),
            Environment.NewLine);

        var result = writer.ToString();

        // assert
        Assert.Equal("single", result);
    }

    [Fact]
    public void SyntaxSerializer_WithIndentation_FormatsCorrectly()
    {
        // arrange
        const string query =
            """
            query GetUser($id: ID!) {
              user(id: $id) {
                name
                email
              }
            }
            """;

        var document = Utf8GraphQLParser.Parse(query);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);
        var result = writer.ToString();

        // assert
        // The result should have proper indentation with each field on its own line
        Assert.Contains($"query GetUser({Environment.NewLine}", result);
        Assert.Contains($"{Environment.NewLine}  $id: ID!{Environment.NewLine}", result);
        Assert.Contains($"  user(id: $id) {{{Environment.NewLine}", result);
        Assert.Contains($"    name{Environment.NewLine}", result);
        Assert.Contains($"    email{Environment.NewLine}", result);
    }

    [Fact]
    public void WriteMany_WithCarriageReturnNewLine_WritesIndentation()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        writer.Indent();
        var items = new[] { "a", "b", "c" };

        // act
        writer.WriteMany(
            items,
            (item, w) => w.Write(item),
            "\r\n");

        var result = writer.ToString();

        // assert
        const string expected = "a\r\n  b\r\n  c";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteMany_WithOnlyCarriageReturn_WritesIndentation()
    {
        // arrange
        var writer = new StringSyntaxWriter();
        writer.Indent();
        var items = new[] { "x", "y" };

        // act
        writer.WriteMany(
            items,
            (item, w) => w.Write(item),
            "\r");

        var result = writer.ToString();

        // assert
        const string expected = "x\r  y";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ObjectValue_Indented_MatchesSnapshot()
    {
        // arrange
        var objectValue = new ObjectValueNode(
            new ObjectFieldNode("enum", new EnumValueNode("Foo")),
            new ObjectFieldNode("enum2", new EnumValueNode("Bar")),
            new ObjectFieldNode("nested", new ObjectValueNode(
                new ObjectFieldNode("inner", "value"))));

        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(objectValue, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void ObjectValue_NotIndented_MatchesSnapshot()
    {
        // arrange
        var objectValue = new ObjectValueNode(
            new ObjectFieldNode("enum", new EnumValueNode("Foo")),
            new ObjectFieldNode("enum2", new EnumValueNode("Bar")),
            new ObjectFieldNode("nested", new ObjectValueNode(
                new ObjectFieldNode("inner", "value"))));

        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = false });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(objectValue, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void ListValue_Indented_MatchesSnapshot()
    {
        // arrange
        var listValue = new ListValueNode(
            new ObjectValueNode(
                new ObjectFieldNode("a", 1),
                new ObjectFieldNode("b", 2)),
            new ObjectValueNode(
                new ObjectFieldNode("c", 3),
                new ObjectFieldNode("d", 4)),
            new IntValueNode(5));

        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(listValue, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void ListValue_NotIndented_MatchesSnapshot()
    {
        // arrange
        var listValue = new ListValueNode(
            new ObjectValueNode(
                new ObjectFieldNode("a", 1),
                new ObjectFieldNode("b", 2)),
            new ObjectValueNode(
                new ObjectFieldNode("c", 3),
                new ObjectFieldNode("d", 4)),
            new IntValueNode(5));

        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = false });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(listValue, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void VariableDefinition_WithDefaultObjectValue_Indented_MatchesSnapshot()
    {
        // arrange
        const string query = """
            query GetUser($input: InputType = { field1: "value1", field2: "value2" }) {
              user
            }
            """;

        var document = Utf8GraphQLParser.Parse(query);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void FieldArgument_WithObjectValue_Indented_MatchesSnapshot()
    {
        // arrange
        const string query = """
            query {
              user(filter: { name: "John", age: 30, active: true })
            }
            """;

        var document = Utf8GraphQLParser.Parse(query);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void ObjectTypeDefinition_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            type User {
              id: ID!
              name: String!
              email: String
              posts: [Post!]!
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void InterfaceTypeDefinition_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            interface Node {
              id: ID!
            }

            type User implements Node {
              id: ID!
              name: String!
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void InputObjectTypeDefinition_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            input UserFilter {
              name: String
              age: Int
              active: Boolean = true
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void EnumTypeDefinition_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            enum Role {
              ADMIN
              USER
              GUEST
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void UnionTypeDefinition_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            union SearchResult = User | Post | Comment
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void TypeWithDirectives_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            type User @auth(requires: ADMIN) {
              id: ID! @deprecated(reason: "Use uid instead")
              uid: ID!
              name: String!
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void ComplexSchemaDefinition_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            schema {
              query: Query
              mutation: Mutation
            }

            type Query {
              user(id: ID!): User
              users(filter: UserFilter): [User!]!
            }

            type Mutation {
              createUser(input: CreateUserInput!): User!
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void QueryWithDirectivesOnField_Indented_MatchesSnapshot()
    {
        // arrange
        const string query = """
            query {
              user(id: "123") {
                id @include(if: true)
                name @skip(if: false) @deprecated
                email @custom(arg1: "value1", arg2: 42)
              }
            }
            """;

        var document = Utf8GraphQLParser.Parse(query);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void FragmentWithDirectives_Indented_MatchesSnapshot()
    {
        // arrange
        const string query = """
            fragment UserFields on User @custom(value: "test") {
              id
              name @include(if: true)
              email
            }

            query {
              user {
                ...UserFields @defer
              }
            }
            """;

        var document = Utf8GraphQLParser.Parse(query);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void InlineFragmentWithDirectives_Indented_MatchesSnapshot()
    {
        // arrange
        const string query = """
            query {
              search {
                ... on User @defer {
                  id
                  name
                }
                ... on Post @defer @stream {
                  title
                  content
                }
              }
            }
            """;

        var document = Utf8GraphQLParser.Parse(query);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void TypeWithManyDirectives_NoWrapping_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            type User @auth @cache @log @validate @track {
              id: ID!
              name: String! @deprecated @private @readonly @indexed @unique
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void TypeWithManyDirectives_WithWrapping_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            type User @auth @cache @log @validate @track {
              id: ID!
              name: String! @deprecated @private @readonly @indexed @unique
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions
        {
            Indented = true,
            MaxDirectivesPerLine = 2
        });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void DirectiveDefinition_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            directive @auth(
              requires: Role = ADMIN
              scopes: [String!]
            ) repeatable on OBJECT | FIELD_DEFINITION

            directive @deprecated(
              reason: String = "No longer supported"
            ) on FIELD_DEFINITION | ENUM_VALUE
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void ArgumentsWithDirectives_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            type Query {
              user(
                id: ID! @deprecated
                filter: UserFilter @custom(value: "test")
              ): User
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void EnumValueWithDirectives_Indented_MatchesSnapshot()
    {
        // arrange
        const string schema = """
            enum Role {
              ADMIN @description(text: "Administrator role")
              USER @description(text: "Regular user")
              GUEST @deprecated(reason: "Use USER instead") @internal
            }
            """;

        var document = Utf8GraphQLParser.Parse(schema);
        var serializer = new SyntaxSerializer(new SyntaxSerializerOptions { Indented = true });
        var writer = new StringSyntaxWriter();

        // act
        serializer.Serialize(document, writer);

        // assert
        writer.ToString().MatchSnapshot(extension: ".graphql");
    }
}
