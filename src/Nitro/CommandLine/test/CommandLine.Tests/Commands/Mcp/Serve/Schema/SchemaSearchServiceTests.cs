using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Schema;

public sealed class SchemaSearchServiceTests
{
    private const string TestSdl = """
        type Query {
          user(id: ID!): User
          users(first: Int): [User!]!
        }

        type User {
          id: ID!
          name: String!
          email: String
        }

        type Post {
          id: ID!
          title: String!
          author: User!
        }

        enum Role {
          ADMIN
          USER
        }

        input CreateUserInput {
          name: String!
        }
        """;

    private readonly SchemaIndex _index = SchemaIndexBuilder.Build(TestSdl);
    private readonly SchemaSearchService _searcher = new();

    [Fact]
    public void Search_ExactNameMatch_Scores_Highest()
    {
        var result = _searcher.Search(_index, "user", null, 100);

        Assert.NotEmpty(result.Results);
        // Both "User" type and "Query.user" field score 100 (exact match).
        // On tie, results are sorted by Coordinate alphabetically.
        // Verify the top results contain "User" type among exact matches.
        var topNames = result.Results
            .TakeWhile((r, i) => i < 3)
            .Select(r => r.Name)
            .ToArray();
        Assert.Contains("User", topNames);
    }

    [Fact]
    public void Search_StartsWith_Match_Returns_Results()
    {
        var result = _searcher.Search(_index, "use", null, 100);

        Assert.NotEmpty(result.Results);
        // "User" starts with "use", "users" starts with "use"
        var names = result.Results.Select(r => r.Name).ToArray();
        Assert.Contains("User", names);
    }

    [Fact]
    public void Search_Contains_Match_Returns_Results()
    {
        var result = _searcher.Search(_index, "ser", null, 100);

        Assert.NotEmpty(result.Results);
        // "User" contains "ser", "users" contains "ser"
        var names = result.Results.Select(r => r.Name).ToArray();
        Assert.Contains("User", names);
    }

    [Fact]
    public void Search_KindFilter_Returns_Only_Fields()
    {
        var result = _searcher.Search(
            _index, "id", SchemaIndexMemberKind.Field, 100);

        Assert.NotEmpty(result.Results);
        Assert.All(result.Results, r => Assert.Equal("FIELD", r.Kind));
    }

    [Fact]
    public void Search_KindFilter_Returns_Only_Types()
    {
        var result = _searcher.Search(
            _index, "user", SchemaIndexMemberKind.Type, 100);

        Assert.NotEmpty(result.Results);
        Assert.All(result.Results, r => Assert.Equal("TYPE", r.Kind));
    }

    [Fact]
    public void Search_Limit_Restricts_Results()
    {
        var result = _searcher.Search(_index, "id", null, 2);

        Assert.True(result.Results.Count <= 2);
        Assert.True(result.TotalCount >= result.Results.Count);
    }

    [Fact]
    public void Search_EmptyQuery_Returns_Results()
    {
        var result = _searcher.Search(_index, "", null, 100);

        // Empty query gives score=1 to all entries, so returns all
        Assert.True(result.Results.Count > 0);
        Assert.Equal(result.TotalCount, result.Results.Count);
    }

    [Fact]
    public void Search_NoMatch_Returns_Empty()
    {
        var result = _searcher.Search(
            _index, "zzzznonexistent", null, 100);

        Assert.Empty(result.Results);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public void Search_Results_Have_PathsToRoot()
    {
        var result = _searcher.Search(
            _index, "name", SchemaIndexMemberKind.Field, 10);

        Assert.NotEmpty(result.Results);
        var userName = result.Results.First(r => r.Coordinate == "User.name");
        // User.name should have a path from Query through some field to User.name
        Assert.NotEmpty(userName.PathsToRoot);
    }

    [Fact]
    public void Search_TotalCount_Reflects_All_Matches()
    {
        var result = _searcher.Search(_index, "id", null, 1);

        // "id" matches multiple entries (User.id, Post.id, Query.user(id:), etc.)
        Assert.True(result.TotalCount > 1);
        Assert.Single(result.Results);
    }

    [Fact]
    public void GetMembers_Returns_Correct_Member_Details()
    {
        var result = _searcher.GetMembers(
            _index, new[] { "User", "User.name" });

        Assert.Equal(2, result.Members.Count);
        Assert.Empty(result.NotFound);

        var userType = result.Members.First(m => m.Coordinate == "User");
        Assert.Equal("TYPE", userType.Kind);

        var userName = result.Members.First(m => m.Coordinate == "User.name");
        Assert.Equal("FIELD", userName.Kind);
        Assert.Equal("String!", userName.TypeName);
    }

    [Fact]
    public void GetMembers_Reports_NotFound_Coordinates()
    {
        var result = _searcher.GetMembers(
            _index, new[] { "User", "NonExistent" });

        Assert.Single(result.Members);
        Assert.Single(result.NotFound);
        Assert.Equal("NonExistent", result.NotFound[0]);
    }
}
