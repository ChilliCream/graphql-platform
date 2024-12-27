using ChilliCream.Testing;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp;

public class DualStoreStarWarsGeneratorTests
{
    [Fact]
    public void Interface_With_Default_Names()
    {
        AssertStarWarsResult(
            new AssertSettings { NoStore = false, SecondaryStore = true },
            @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
                        appearsIn
                    }
                }");
    }

    [Fact]
    public void Operation_With_Leaf_Argument()
    {
        AssertStarWarsResult(
            new AssertSettings { NoStore = false, SecondaryStore = true },
            @"query GetHero($episode: Episode) {
                    hero(episode: $episode) {
                        name
                        appearsIn
                    }
                }");
    }

    [Fact]
    public void Operation_With_Type_Argument()
    {
        AssertStarWarsResult(
            new AssertSettings { NoStore = false, SecondaryStore = true },
            @"mutation createReviewMut($episode: Episode!, $review: ReviewInput!) {
                    createReview(episode: $episode, review: $review) {
                        stars
                        commentary
                    }
                }");
    }

    [Fact]
    public void Interface_With_Fragment_Definition_Two_Models()
    {
        AssertStarWarsResult(
            new AssertSettings { NoStore = false, SecondaryStore = true },
            @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        ... Hero
                    }
                }

                fragment Hero on Character {
                    name
                    ... Human
                    ... Droid
                    friends {
                        nodes {
                            name
                        }
                    }
                }

                fragment Human on Human {
                    homePlanet
                }

                fragment Droid on Droid {
                    primaryFunction
                }");
    }

    [Fact]
    public void Subscription_With_Default_Names()
    {
        AssertStarWarsResult(
            new AssertSettings { NoStore = false, SecondaryStore = true },
            @"subscription OnReviewSub {
                    onReview(episode: NEW_HOPE) {
                        stars
                        commentary
                    }
                }");
    }

    [Fact]
    public void Generate_StarWarsIntegrationTest()
    {
        AssertStarWarsResult(
            new AssertSettings { NoStore = false, SecondaryStore = true },
            FileResource.Open("QueryWithSubscription.graphql"));
    }

    [Fact]
    public void StarWarsTypeNameOnUnions() =>
        AssertStarWarsResult(
            new AssertSettings { NoStore = false, SecondaryStore = true },
            @"query SearchHero {
                    search(text: ""l"") {
                        __typename
                    }
                }");

    [Fact]
    public void StarWarsUnionList() =>
        AssertStarWarsResult(
            new AssertSettings { NoStore = false, SecondaryStore = true },
            @"query SearchHero {
                    search(text: ""l"") {
                        ... on Human {
                            name
                        }
                        ... on Droid {
                            name
                        }
                    }
                }");
}
