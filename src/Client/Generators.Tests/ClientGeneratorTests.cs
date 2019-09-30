using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Generators
{
    public class ClientGeneratorTests
        : ModelGeneratorTestBase
    {
        [Fact]
        public async Task NonNull_Object_List()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo: Foo!
                }

                type Foo {
                    bars: [Bar!]!
                }

                type Bar {
                    baz: String!
                }
                ";

            string query =
               @"
                query getBars {
                    foo {
                        bars {
                            baz
                        }
                    }
                }
                ";

            // act
            await ClientGenerator.New()
                .AddQueryDocumentFromString("Queries", query)
                .AddSchemaDocumentFromString("Schema", schema)
                .SetOutput(outputHandler)
                .BuildAsync();

            // assert
            outputHandler.Content.MatchSnapshot();
        }

        [Fact]
        public async Task NonNull_Scalar_List()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo: Foo!
                }

                type Foo {
                    bars: [String!]!
                }
                ";

            string query =
               @"
                query getBars {
                    foo {
                        bars
                    }
                }
                ";

            // act
            await ClientGenerator.New()
                .AddQueryDocumentFromString("Queries", query)
                .AddSchemaDocumentFromString("Schema", schema)
                .SetOutput(outputHandler)
                .BuildAsync();

            // assert
            outputHandler.Content.MatchSnapshot();
        }

        [Fact]
        public async Task Two_Alias_Fields()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo: Foo!
                }

                type Foo {
                    bar: String
                }
                ";

            string query =
               @"
                query getBars {
                    a: foo {
                        bar
                    }
                    b: foo {
                        bar
                    }
                }
                ";

            // act
            await ClientGenerator.New()
                .AddQueryDocumentFromString("Queries", query)
                .AddSchemaDocumentFromString("Schema", schema)
                .SetOutput(outputHandler)
                .BuildAsync();

            // assert
            outputHandler.Content.MatchSnapshot();
        }
    }
}
