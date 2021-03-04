using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp.Integration
{
    public class TestGeneration
    {
        [Fact]
        public void StarWarsGetHero() =>
            AssertStarWarsResult(
                CreateIntegrationTest(),
                @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
                    }
                }");

        [Fact]
        public void StarWarsGetFriends() =>
            AssertStarWarsResult(
                CreateIntegrationTest(),
                @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
                        friends {
                            nodes {
                                name
                            }
                        }
                    }
                }");
    }
}
