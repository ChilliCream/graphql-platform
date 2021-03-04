using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class StarWarsGeneratorTests
    {
        [Fact]
        public void Interface_With_Default_Names()
        {
            AssertStarWarsResult(
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
                FileResource.Open("QueryWithSubscription.graphql"));
        }
    }
}
