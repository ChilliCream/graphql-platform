namespace HotChocolate.Types.SDL;

public class InputObjectTypeSchemaFirstTests
{
    [Fact]
    public void Declare_Simple_Input_Type()
    {
        // arrange
        const string sdl =
            @"type Query {
                    hello(input: HelloInput): String
                }

                input HelloInput {
                    hello: String
                }";

        // act
        // assert
        SchemaBuilder.New()
            .AddDocumentFromString(sdl)
            .BindRuntimeType<Query>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Declare_Input_Type_With_Type_Extension()
    {
        // arrange
        const string sdl =
            @"type Query {
                    hello(input: HelloInput): String
                }

                input HelloInput {
                    hello: String
                }

                extend input HelloInput {
                    world: String
                }";

        // act
        // assert
        SchemaBuilder.New()
            .AddDocumentFromString(sdl)
            .BindRuntimeType<Query>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Declare_Query_Type_With_Type_Extension_Add_Directive_To_Field()
    {
        // arrange
        const string sdl =
            @"type Query {
                    hello(input: HelloInput): String
                }

                input HelloInput {
                    hello: String
                }

                extend input HelloInput {
                    world: String @foo
                }

                directive @foo on INPUT_FIELD_DEFINITION";

        // act
        // assert
        SchemaBuilder.New()
            .AddDocumentFromString(sdl)
            .BindRuntimeType<Query>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Declare_Query_Type_With_Type_Extension_Add_Directive_To_Type()
    {
        // arrange
        const string sdl =
            @"type Query {
                    hello(input: HelloInput): String
                }

                input HelloInput {
                    hello: String
                }

                extend input HelloInput @foo

                directive @foo on INPUT_OBJECT";

        // act
        // assert
        SchemaBuilder.New()
            .AddDocumentFromString(sdl)
            .BindRuntimeType<Query>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    public class Query
    {
        public string Hello(HelloInput input) => "Hello";
    }

    public class HelloInput
    {
        public string Hello { get; set; }
        public string World { get; set; }
    }
}
