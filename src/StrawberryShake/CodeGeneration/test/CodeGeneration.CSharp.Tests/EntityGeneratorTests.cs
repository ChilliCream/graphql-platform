using ChilliCream.Testing;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp;

public class EntityGeneratorTests
{
    [Fact]
    public void Generate_ChatClient_ConnectionNotAnEntity()
    {
        AssertResult(
            FileResource.Open("ChatPeopleNodes.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("ChatSchema.graphql"));
    }

    [Fact]
    public void Generate_ChatClient_ConnectionNotAnEntity_With_Records()
    {
        AssertResult(
            settings: new AssertSettings { EntityRecords = true, },
            FileResource.Open("ChatPeopleNodes.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("ChatSchema.graphql"));
    }

    [Fact]
    public void Generate_ChatClient_MapperMapsEntityOnRootCorrectly()
    {
        AssertResult(
            FileResource.Open("ChatSendMessage.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("ChatSchema.graphql"));
    }

    [Fact]
    public void Generate_ChatClient_MapperMapsEntityOnRootCorrectly_With_Records()
    {
        AssertResult(
            settings: new AssertSettings { EntityRecords = true, },
            FileResource.Open("ChatSendMessage.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("ChatSchema.graphql"));
    }

    [Fact]
    public void Generate_BookClient_DataOnly_UnionDataTypes()
    {
        AssertResult(
            FileResource.Open("BookUnionQuery.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("BookSchema.graphql"));
    }

    [Fact]
    public void Generate_BookClient_DataOnly_UnionDataTypes_With_Records()
    {
        AssertResult(
            settings: new AssertSettings { EntityRecords = true, },
            FileResource.Open("BookUnionQuery.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("BookSchema.graphql"));
    }

    [Fact]
    public void Generate_BookClient_DataOnly_InterfaceDataTypes()
    {
        AssertResult(
            FileResource.Open("BookInterfaceQuery.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("BookSchema.graphql"));
    }

    [Fact]
    public void Generate_BookClient_DataOnly_InterfaceDataTypes_With_Records()
    {
        AssertResult(
            settings: new AssertSettings { EntityRecords = true, },
            FileResource.Open("BookInterfaceQuery.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("BookSchema.graphql"));
    }

    [Fact]
    public void Generate_BookClient_DataInEntity_UnionDataTypes()
    {
        AssertResult(
            FileResource.Open("BookUnionQueryWithEntity.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("BookSchema.graphql"));
    }

    [Fact]
    public void Generate_BookClient_DataInEntity_UnionDataTypes_With_Records()
    {
        AssertResult(
            settings: new AssertSettings { EntityRecords = true, },
            FileResource.Open("BookUnionQueryWithEntity.graphql"),
            FileResource.Open("Schema.extensions.graphql"),
            FileResource.Open("BookSchema.graphql"));
    }

    [Fact(Skip = "We are postponing the defer feature until the spec is more stable.")]
    public void Generate_StarWars_Client_With_Defer()
    {
        AssertStarWarsResult(
            @"query GetHero {
                hero(episode: NEW_HOPE) {
                    ... HeroName
                    ... HeroAppearsIn @defer(label: ""HeroAppearsInAbc"")
                }
            }

            fragment HeroName on Character {
                name
                friends {
                    nodes {
                        name
                        ... HeroAppearsIn2 @defer(label: ""HeroAppearsIn2"")
                    }
                }
            }

            fragment HeroAppearsIn on Character {
                appearsIn
            }

            fragment HeroAppearsIn2 on Character {
                appearsIn
            }");
    }
}
