using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;
using HotChocolate.Language;

using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Schema;

public sealed class SchemaIndexBuilderTests
{
    private const string TestSdl = """
        type Query {
          user(id: ID!): User
          users(first: Int, after: String): [User!]!
        }

        type User {
          id: ID!
          name: String!
          email: String @deprecated(reason: "Use contacts instead")
          posts: [Post!]!
        }

        type Post {
          id: ID!
          title: String!
          content: String
          author: User!
        }

        input CreateUserInput {
          name: String!
          email: String!
        }

        enum Role {
          ADMIN
          USER
          MODERATOR
        }

        interface Node {
          id: ID!
        }
        """;

    private readonly SchemaIndex _index = SchemaIndexBuilder.Build(TestSdl);

    [Fact]
    public void Build_Creates_Correct_Total_Entry_Count()
    {
        // Types: Query, User, Post, CreateUserInput, Role, Node = 6
        // Fields: Query.user, Query.users, User.id, User.name, User.email, User.posts,
        //         Post.id, Post.title, Post.content, Post.author, Node.id = 11
        // Arguments: Query.user(id), Query.users(first), Query.users(after) = 3
        // EnumValues: Role.ADMIN, Role.USER, Role.MODERATOR = 3
        // InputFields: CreateUserInput.name, CreateUserInput.email = 2
        // Total = 6 + 11 + 3 + 3 + 2 = 25
        Assert.Equal(25, _index.Count);
    }

    [Theory]
    [InlineData("Query", "Type")]
    [InlineData("User", "Type")]
    [InlineData("Post", "Type")]
    [InlineData("CreateUserInput", "InputType")]
    [InlineData("Role", "Enum")]
    [InlineData("Node", "Interface")]
    public void Build_Creates_Type_Entries(string coordinate, string expectedKind)
    {
        var entry = _index.GetByCoordinate(coordinate);

        Assert.NotNull(entry);
        Assert.Equal(Enum.Parse<SchemaIndexMemberKind>(expectedKind), entry.Kind);
        Assert.Equal(coordinate, entry.Name);
    }

    [Theory]
    [InlineData("User.name", "name", "String!")]
    [InlineData("User.email", "email", "String")]
    [InlineData("Post.title", "title", "String!")]
    [InlineData("Post.author", "author", "User!")]
    [InlineData("Query.user", "user", "User")]
    [InlineData("Query.users", "users", "[User!]!")]
    public void Build_Creates_Field_Entries(
        string coordinate, string expectedName, string expectedTypeName)
    {
        var entry = _index.GetByCoordinate(coordinate);

        Assert.NotNull(entry);
        Assert.Equal(SchemaIndexMemberKind.Field, entry.Kind);
        Assert.Equal(expectedName, entry.Name);
        Assert.Equal(expectedTypeName, entry.TypeName);
    }

    [Theory]
    [InlineData("Query.user(id:)", "id", "ID!")]
    [InlineData("Query.users(first:)", "first", "Int")]
    [InlineData("Query.users(after:)", "after", "String")]
    public void Build_Creates_Argument_Entries(
        string coordinate, string expectedName, string expectedTypeName)
    {
        var entry = _index.GetByCoordinate(coordinate);

        Assert.NotNull(entry);
        Assert.Equal(SchemaIndexMemberKind.Argument, entry.Kind);
        Assert.Equal(expectedName, entry.Name);
        Assert.Equal(expectedTypeName, entry.TypeName);
    }

    [Theory]
    [InlineData("Role.ADMIN", "ADMIN")]
    [InlineData("Role.USER", "USER")]
    [InlineData("Role.MODERATOR", "MODERATOR")]
    public void Build_Creates_EnumValue_Entries(string coordinate, string expectedName)
    {
        var entry = _index.GetByCoordinate(coordinate);

        Assert.NotNull(entry);
        Assert.Equal(SchemaIndexMemberKind.EnumValue, entry.Kind);
        Assert.Equal(expectedName, entry.Name);
        Assert.Equal("Role", entry.ParentTypeName);
    }

    [Theory]
    [InlineData("CreateUserInput.name", "name", "String!")]
    [InlineData("CreateUserInput.email", "email", "String!")]
    public void Build_Creates_InputField_Entries(
        string coordinate, string expectedName, string expectedTypeName)
    {
        var entry = _index.GetByCoordinate(coordinate);

        Assert.NotNull(entry);
        Assert.Equal(SchemaIndexMemberKind.InputField, entry.Kind);
        Assert.Equal(expectedName, entry.Name);
        Assert.Equal(expectedTypeName, entry.TypeName);
        Assert.Equal("CreateUserInput", entry.ParentTypeName);
    }

    [Fact]
    public void Build_Deprecated_Field_Has_Deprecation_Reason()
    {
        var entry = _index.GetByCoordinate("User.email");

        Assert.NotNull(entry);
        Assert.True(entry.IsDeprecated);
        Assert.Equal("Use contacts instead", entry.DeprecationReason);
    }

    [Fact]
    public void Build_NonDeprecated_Field_Has_No_Deprecation()
    {
        var entry = _index.GetByCoordinate("User.name");

        Assert.NotNull(entry);
        Assert.False(entry.IsDeprecated);
        Assert.Null(entry.DeprecationReason);
    }

    [Theory]
    [InlineData("Query", "Query")]
    [InlineData("User.name", "User.name")]
    [InlineData("Query.user(id:)", "Query.user(id:)")]
    [InlineData("Role.ADMIN", "Role.ADMIN")]
    [InlineData("CreateUserInput.name", "CreateUserInput.name")]
    public void Build_Coordinate_Format_Is_Correct(string coordinate, string expected)
    {
        var entry = _index.GetByCoordinate(coordinate);

        Assert.NotNull(entry);
        Assert.Equal(expected, entry.Coordinate);
    }

    [Fact]
    public void Build_Field_Has_Correct_ParentTypeName()
    {
        var entry = _index.GetByCoordinate("User.posts");

        Assert.NotNull(entry);
        Assert.Equal("User", entry.ParentTypeName);
    }

    [Fact]
    public void Build_Argument_Has_FieldCoordinate_As_ParentTypeName()
    {
        var entry = _index.GetByCoordinate("Query.user(id:)");

        Assert.NotNull(entry);
        Assert.Equal("Query.user", entry.ParentTypeName);
    }

    [Fact]
    public void Build_RootTypes_Contains_Query()
    {
        Assert.Contains("Query", _index.RootTypes);
    }

    [Fact]
    public void Build_GetChildCoordinates_Returns_Fields_Of_Type()
    {
        var children = _index.GetChildCoordinates("User");

        Assert.Contains("User.id", children);
        Assert.Contains("User.name", children);
        Assert.Contains("User.email", children);
        Assert.Contains("User.posts", children);
        Assert.Equal(4, children.Count);
    }

    [Fact]
    public void Build_GetChildCoordinates_Returns_EnumValues()
    {
        var children = _index.GetChildCoordinates("Role");

        Assert.Contains("Role.ADMIN", children);
        Assert.Contains("Role.USER", children);
        Assert.Contains("Role.MODERATOR", children);
        Assert.Equal(3, children.Count);
    }

    [Fact]
    public void Build_GetIncomingEdges_Returns_Fields_Referencing_Type()
    {
        var incoming = _index.GetIncomingEdges("User");

        Assert.Contains("Query.user", incoming);
        Assert.Contains("Query.users", incoming);
        Assert.Contains("Post.author", incoming);
    }

    [Fact]
    public void Build_Field_With_Arguments_Has_Arguments_Property()
    {
        var entry = _index.GetByCoordinate("Query.user");

        Assert.NotNull(entry);
        Assert.NotNull(entry.Arguments);
        Assert.Single(entry.Arguments);
        Assert.Equal("id", entry.Arguments[0].Name);
        Assert.Equal("ID!", entry.Arguments[0].TypeName);
    }

    [Fact]
    public void Build_From_DocumentNode_Works()
    {
        var document = Utf8GraphQLParser.Parse(TestSdl);
        var index = SchemaIndexBuilder.Build(document);

        Assert.Equal(25, index.Count);
    }

    [Fact]
    public void Build_Enum_Entry_Has_EnumValues_Property()
    {
        var entry = _index.GetByCoordinate("Role");

        Assert.NotNull(entry);
        Assert.NotNull(entry.EnumValues);
        Assert.Equal(3, entry.EnumValues.Count);
        Assert.Contains(entry.EnumValues, v => v.Name == "ADMIN");
        Assert.Contains(entry.EnumValues, v => v.Name == "USER");
        Assert.Contains(entry.EnumValues, v => v.Name == "MODERATOR");
    }
}
