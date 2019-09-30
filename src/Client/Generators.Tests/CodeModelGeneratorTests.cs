using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Generators
{
    public class CodeModelGeneratorTests
        : ModelGeneratorTestBase
    {
        [Fact]
        public async Task NonNull_Objects_With_Lists()
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
                    baz: String
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
    }
}
