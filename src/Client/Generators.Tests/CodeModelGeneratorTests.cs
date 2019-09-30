using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using Snapshooter;
using Snapshooter.Xunit;
using StrawberryShake.Generators.CSharp;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using Xunit;

namespace StrawberryShake.Generators
{
    public class CodeModelGeneratorTests
        : ModelGeneratorTestBase
    {
        [Fact]
        public async Task Objects_With_Lists()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo: Foo
                }

                type Foo {
                    bars: [Bar]
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
