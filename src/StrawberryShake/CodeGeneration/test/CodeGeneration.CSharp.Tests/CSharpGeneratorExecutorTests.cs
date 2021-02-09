using System.Text;
using System.Threading.Tasks;
using HotChocolate.Types;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorExecutorTests
    {
        [Fact]
        public async Task Interface_With_Default_Names()
        {
            // arrange
            ClientModel clientModel =
                await TestHelper.CreateClientModelAsync(
                    @"query GetHero {
                        hero(episode: NEW_HOPE) {
                            name
                            appearsIn
                        }
                    }",
                    "extend schema @key(fields: \"id\")");

            // act
            var documents = new StringBuilder();
            var generator = new CSharpGeneratorExecutor();

            // assert
            AssertResult(clientModel, generator, documents);
        }

        [Fact]
        public async Task Operation_With_Leaf_Argument()
        {
            // arrange
            ClientModel clientModel =
                await TestHelper.CreateClientModelAsync(
                    @"
                    query GetHero($episode: Episode) {
                        hero(episode: $episode) {
                            name
                            appearsIn
                        }
                    }
                    ",
                    "extend schema @key(fields: \"id\")");

            // act
            var documents = new StringBuilder();
            var generator = new CSharpGeneratorExecutor();

            // assert
            AssertResult(clientModel, generator, documents);
        }

        [Fact]
        public async Task Operation_With_Type_Argument()
        {
            // arrange
            ClientModel clientModel =
                await TestHelper.CreateClientModelAsync(
                    @"
                    mutation createReviewMut($episode: Episode!, $review: ReviewInput!) {
                      createReview(episode: $episode, review: $review) {
                        stars
                        commentary
                      }
                    }
                    ",
                    "extend schema @key(fields: \"id\")");

            // act
            var documents = new StringBuilder();
            var generator = new CSharpGeneratorExecutor();

            // assert
            AssertResult(clientModel, generator, documents);
        }

        [Fact]
        public async Task Interface_With_Fragment_Definition_Two_Models()
        {
            // arrange
            ClientModel clientModel =
                await TestHelper.CreateClientModelAsync(
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
                    }",
                    "extend schema @key(fields: \"id\")");

            // act
            var documents = new StringBuilder();
            var generator = new CSharpGeneratorExecutor();

            // assert
            AssertResult(clientModel, generator, documents);
        }

        [Fact]
        public async Task Subscription_With_Default_Names()
        {
            // arrange
            ClientModel clientModel =
                await TestHelper.CreateClientModelAsync(
                    @"subscription OnReviewSub {
                        onReview(episode: NEW_HOPE) {
                            stars
                            commentary
                        }
                    }",
                    "extend schema @key(fields: \"id\")");

            // act
            var documents = new StringBuilder();
            var generator = new CSharpGeneratorExecutor();

            // assert
            AssertResult(clientModel, generator, documents);
        }

        [Fact]
        public void Operation_With_Complex_Arguments()
        {
            // arrange
            ClientModel clientModel = new DocumentAnalyzer()
                .SetSchema(
                    SchemaHelper.Load(
                        ("",
                            Utf8GraphQLParser.Parse(@"
                            schema {
                                query: Query
                            }

                            type Query {
                                foo(single: Bar!, list: [Bar!]!, nestedList: [[Bar]]): String
                            }

                            input Bar {
                                str: String
                                strNonNullable: String!
                                nested: Bar
                                nestedList: [Bar!]!
                                nestedMatrix: [[Bar]]
                            }"))
                    ))
                .AddDocument(
                    Utf8GraphQLParser.Parse(
                        @"
                        query test($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                          foo(single: $single, list: $list, nestedList:$nestedList)
                        }
                    "))
                .AddDocument(Utf8GraphQLParser.Parse("extend schema @key(fields: \"id\")"))
                .Analyze();

            // act
            var documents = new StringBuilder();
            var generator = new CSharpGeneratorExecutor();

            // assert
            AssertResult(clientModel, generator, documents);
        }

        private static void AssertResult(
            ClientModel clientModel,
            CSharpGeneratorExecutor generator,
            StringBuilder documents)
        {
            foreach (CSharpDocument document in generator.Generate(clientModel, "Foo", "FooClient"))
            {
                documents.AppendLine("// " + document.Name);
                documents.AppendLine();
                documents.AppendLine(document.SourceText);
                documents.AppendLine();
            }

            documents.ToString().MatchSnapshot();
        }
    }
}
