using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.SDL
{
    public class ObjectTypeSchemaFirstTests
    {
        [Fact]
        public void Declare_Simple_Query_Type()
        {
            // arrange
            var sdl =
                @"type Query {
                    hello: String
                }";

            // act
            // assert
            SchemaBuilder.New()
                .AddDocumentFromString(sdl)
                .BindComplexType<Query>()
                .Create()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void Declare_Query_Type_With_Type_Extension()
        {
            // arrange
            var sdl =
                @"type Query {
                    hello: String
                }

                extend type Query {
                    world: String
                }";

            // act
            // assert
            SchemaBuilder.New()
                .AddDocumentFromString(sdl)
                .BindComplexType<Query>()
                .Create()
                .Print()
                .MatchSnapshot();
        }

        public class Query
        {
            public string Hello() => "Hello";

            public string World() => "World";
        }
    }
}
