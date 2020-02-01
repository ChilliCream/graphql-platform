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
        public async Task Nested_List_Nullable_ReturnType()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo(a: String): [String]
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
        public async Task Nested_List_ReturnType()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo(a: String): [String!]!
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

        [Fact]
        public async Task Enum_As_Output_Field_Return_Type()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo: Foo
                }

                type Foo {
                    bar2: Bar
                    bar1: Bar!
                    bar3: [Bar]
                    bar4: [Bar]!
                    bar5: [Bar!]
                    bar6: [Bar!]!
                }

                enum Bar {
                    ABC
                }
                ";

            string query =
               @"
                query getFoo {
                    foo {
                        bar1
                        bar2
                        bar3
                        bar4
                        bar5
                        bar6
                    }
                }
                ";

            // act
            await ClientGenerator.New()
                .AddQueryDocumentFromString("Queries", query)
                .AddSchemaDocumentFromString("Schema", schema)
                .SetOutput(outputHandler)
                .ModifyOptions(o => o.LanguageVersion = LanguageVersion.CSharp_8_0)
                .BuildAsync();

            // assert
            outputHandler.Content.MatchSnapshot();
        }

        [Fact]
        public async Task Return_Type_Renamed()
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
                    foo @type(name: ""FooNew"") {
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
        public async Task Input_Objects_Arguments()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo(input: FooInput!): String
                }

                input FooInput {
                    bar: BarInput
                }

                input BarInput {
                    baz: Int
                }
                ";

            string query =
               @"
                query getFoo($input: FooInput!) {
                    foo(input: $input)
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
        public async Task Two_Input_Objects_Arguments()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo(input: FooInput!): String
                }

                input FooInput {
                    bar: BarInput
                }

                input BarInput {
                    baz: Int
                }
                ";

            string query =
               @"
                query getFoo($input1: FooInput! $input2: FooInput!) {
                    a: foo(input: $input1)
                    b: foo(input: $input2)
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
        public async Task Custom_Scalar_Types()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo: Bar
                    baz: Qux
                    abc: String
                }

                scalar String
                scalar Bar
                scalar Qux
                ";

            string extensions = @"
                extend scalar Bar @runtimeType(name: ""System.String"")
                extend scalar Qux @runtimeType(name: ""System.Int32"")";

            string query =
               @"
                query getFoo {
                    foo
                    baz
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

        [Fact]
        public async Task Custom_Scalar_Types_Byte_Array()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo: Bar
                    baz: Qux
                    abc: String
                }

                scalar String
                scalar Bar
                scalar Qux
                ";

            string extensions = @"
                extend scalar Bar
                    @runtimeType(name: ""System.String[]"")
                    @serializationType(name: ""System.String"")
                extend scalar Qux
                    @runtimeType(name: ""System.Byte[]"")
                    @serializationType(name: ""System.String"")";

            string query =
               @"
                query getFoo {
                    foo
                    baz
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

        [Fact]
        public async Task Custom_Scalar_Types_Byte_Array_LanguageVersion()
        {
            // arrange
            var outputHandler = new TestOutputHandler();

            string schema = @"
                type Query {
                    foo: Bar
                    baz: Qux
                    abc: String
                }

                scalar String
                scalar Bar
                scalar Qux
                ";

            string extensions = @"
                extend scalar Bar
                    @runtimeType(name: ""System.String[]"")
                    @serializationType(name: ""System.String"")
                extend scalar Qux
                    @runtimeType(name: ""System.Byte[]"")
                    @serializationType(name: ""System.String"")";

            string query =
               @"
                query getFoo {
                    foo
                    baz
                }
                ";

            // act
            await ClientGenerator.New()
                .AddQueryDocumentFromString("Queries", query)
                .AddSchemaDocumentFromString("Schema", schema)
                .AddSchemaDocumentFromString("Extensions", extensions)
                .ModifyOptions(o => o.LanguageVersion = LanguageVersion.CSharp_7_3)
                .SetOutput(outputHandler)
                .BuildAsync();

            // assert
            outputHandler.Content.MatchSnapshot();
        }
    }
}
