using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.Generators
{
    public class ClientGeneratorTests
        : ModelGeneratorTestBase
    {
        [Fact]
        public async Task Single_Nullable_Scalar_Argument()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo(a: String): String
                }
                ";

            string query =
               @"
                query getBars($a: String) {
                    foo(a: $a)
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
        public async Task Two_Nullable_Scalar_Arguments()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo(a: String b: Int): String
                }
                ";

            string query =
               @"
                query getBars($a: String $b: Int) {
                    foo(a: $a b: $b)
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

        [Fact]
        public async Task Enum_Type()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    appearsIn: [Episode]
                }

                enum Episode {
                    NEWHOPE
                    EMPIRE
                    JEDI
                }
                ";

            string query =
               @"
                query getEpisode {
                    appearsIn
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
        public async Task Enum_Type_Set_Value_Name()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    appearsIn: [Episode]
                }

                enum Episode {
                    NEWHOPE
                    EMPIRE
                    JEDI
                }
                ";

            string extensions = @"
                extend enum Episode {
                    NEWHOPE @name(value: ""NewHope"")
                }
                ";

            string query =
               @"
                query getEpisode {
                    appearsIn
                }
                ";

            // act
            await ClientGenerator.New()
                .AddQueryDocumentFromString("Queries", query)
                .AddSchemaDocumentFromString("Schema", schema)
                .AddSchemaDocumentFromString("Extensions", extensions)
                .SetOutput(outputHandler)
                .BuildAsync();

            // assert
            outputHandler.Content.MatchSnapshot();
        }
    }
}
