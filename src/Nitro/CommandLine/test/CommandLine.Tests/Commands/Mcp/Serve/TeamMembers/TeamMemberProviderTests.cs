using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Services;

using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.TeamMembers.Models;
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.TeamMembers;

public sealed class TeamMemberProviderTests
{
    private readonly TeamMemberProvider _provider = TeamMemberProvider.Instance;

    private static readonly string[] AllMemberKeys =
    [
        "backend_engineer",
        "frontend_engineer",
        "graphql_expert",
        "schema_reviewer",
        "performance_engineer",
        "devops_engineer",
        "security_engineer",
        "testing_engineer",
        "relay_expert"
    ];

    [Fact]
    public void GetAll_ReturnsAll9Members()
    {
        // act
        var members = _provider.GetAll().ToList();

        // assert
        Assert.Equal(9, members.Count);
    }

    [Theory]
    [InlineData("backend_engineer")]
    [InlineData("frontend_engineer")]
    [InlineData("graphql_expert")]
    [InlineData("schema_reviewer")]
    [InlineData("performance_engineer")]
    [InlineData("devops_engineer")]
    [InlineData("security_engineer")]
    [InlineData("testing_engineer")]
    [InlineData("relay_expert")]
    public void GetById_KnownKey_ReturnsMember(string key)
    {
        // act
        var member = _provider.GetById(key);

        // assert
        Assert.NotNull(member);
        Assert.Equal(key, member.Id);
    }

    [Fact]
    public void GetById_UnknownKey_ReturnsNull()
    {
        // act
        var member = _provider.GetById("nonexistent_member");

        // assert
        Assert.Null(member);
    }

    [Fact]
    public void NoDuplicateMemberKeys()
    {
        // act
        var members = _provider.GetAll().ToList();
        var ids = members.Select(m => m.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();

        // assert
        Assert.Equal(uniqueIds.Count, ids.Count);
    }

    [Theory]
    [InlineData("backend_engineer")]
    [InlineData("frontend_engineer")]
    [InlineData("graphql_expert")]
    [InlineData("schema_reviewer")]
    [InlineData("performance_engineer")]
    [InlineData("devops_engineer")]
    [InlineData("security_engineer")]
    [InlineData("testing_engineer")]
    [InlineData("relay_expert")]
    public void EachMember_HasNonEmptyContent(string key)
    {
        // act
        var member = _provider.GetById(key);

        // assert
        Assert.NotNull(member);
        Assert.False(string.IsNullOrWhiteSpace(member.Id), "Id should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(member.Title), "Title should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(member.Description),
            "Description should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(member.PersonaText),
            "PersonaText should not be empty.");
    }

    [Theory]
    [InlineData("backend_engineer")]
    [InlineData("frontend_engineer")]
    [InlineData("graphql_expert")]
    [InlineData("schema_reviewer")]
    [InlineData("performance_engineer")]
    [InlineData("devops_engineer")]
    [InlineData("security_engineer")]
    [InlineData("testing_engineer")]
    [InlineData("relay_expert")]
    public void EachMember_HasExpectedSections(string key)
    {
        // act
        var member = _provider.GetById(key);

        // assert
        Assert.NotNull(member);
        Assert.Contains("Identity", member.PersonaText);
        Assert.Contains("Core Expertise", member.PersonaText);
    }

    [Fact]
    public void AllExpectedKeys_ArePresent()
    {
        // act
        var members = _provider.GetAll().ToList();
        var memberIds = members.Select(m => m.Id).ToHashSet();

        // assert
        foreach (var key in AllMemberKeys)
        {
            Assert.Contains(key, memberIds);
        }
    }

    [Fact]
    public void GetAll_MembersHaveDistinctTitles()
    {
        // act
        var members = _provider.GetAll().ToList();
        var titles = members.Select(m => m.Title).ToList();
        var uniqueTitles = titles.Distinct().ToList();

        // assert
        Assert.Equal(uniqueTitles.Count, titles.Count);
    }
}
