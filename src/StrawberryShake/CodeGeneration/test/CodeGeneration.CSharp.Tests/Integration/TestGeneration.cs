using ChilliCream.Testing;
using StrawberryShake.Tools.Configuration;
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
        public void StarWarsGetFriendsNoStore() =>
            AssertStarWarsResult(
                CreateIntegrationTest(noStore: true),
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

        [Fact]
        public void MultiProfile() =>
            AssertStarWarsResult(
                CreateIntegrationTest(profiles: new[]
                {
                    new TransportProfile("InMemory", TransportType.InMemory),
                    TransportProfile.Default
                }),
                @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        name
                        friends {
                            nodes {
                                name
                            }
                        }
                    }
                }",
                @"subscription OnReviewSub {
                    onReview(episode: NEW_HOPE) {
                      __typename
                      stars
                      commentary
                    }
                  }
                ",
                @"mutation createReviewMut($episode: Episode!, $review: ReviewInput!) {
                    createReview(episode: $episode, review: $review) {
                        stars
                        commentary
                    }
                }");

        [Fact]
        public void StarWarsTypeNameOnInterfaces() =>
            AssertStarWarsResult(
                CreateIntegrationTest(),
                @"query GetHero {
                    hero(episode: NEW_HOPE) {
                        __typename
                    }
                }");

        [Fact]
        public void StarWarsTypeNameOnUnions() =>
            AssertStarWarsResult(
                CreateIntegrationTest(),
                @"query SearchHero {
                    search(text: ""l"") {
                        __typename
                    }
                }");

        [Fact]
        public void StarWarsUnionList() =>
            AssertStarWarsResult(
                CreateIntegrationTest(),
                @"query SearchHero {
                    search(text: ""l"") {
                        ... on Human {
                            name
                            friends {
                                nodes {
                                    name
                                }
                            }
                        }
                        ... on Droid {
                            name
                        }
                    }
                }");

        [Fact]
        public void EntityIdOrData() =>
            AssertResult(
                CreateIntegrationTest(profiles: new[]
                {
                    new TransportProfile("Default", TransportType.InMemory)
                }),
                true,
                @"
                query GetFoo {
                    foo {
                        ... on Baz {
                            id
                        }
                        ... on Quox {
                            foo
                        }
                        ... on Baz2 {
                            id
                        }
                        ... on Quox2 {
                            foo
                        }
                    }
                }
                ",
                @"
                type Query {
                    foo: [Bar]
                }

                type Baz {
                    id: String
                }

                type Baz2 {
                    id: String
                }

                type Quox {
                    foo: String
                }

                type Quox2 {
                    foo: String
                }

                union Bar = Baz | Quox | Baz2 | Quox2
                ",
                "extend schema @key(fields: \"id\")");

        [Fact]
        public void StarWarsIntrospection() =>
            AssertStarWarsResult(
                CreateIntegrationTest(),
                FileResource.Open("IntrospectionQuery.graphql"));
    }
}
