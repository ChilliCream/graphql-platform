using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;
using System.Threading.Tasks;
using Snapshooter;

namespace HotChocolate.Types.SDL
{
    public class EnumTypeSchemaFirstTests
    {
        [Fact]
        public void Declare_EnumType_With_Explicit_Value_Binding()
        {
            // arrange
            var sdl =
                @"type Query {
                    hello(greetings: Greetings): Greetings
                }

                enum Greetings {
                    GOOD @bind(to: ""GoodMorning"")
                }";

            // act
            // assert
            SchemaBuilder.New()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<Query>()
                .Create()
                .MakeExecutable()
                .Execute("{ hello(greetings: GOOD) }")
                .ToJson()
                .MatchSnapshot();
        }

        [Fact]
        public void Declare_EnumType_With_Implicit_Value_Binding()
        {
            // arrange
            var sdl =
                @"type Query {
                    hello(greetings: Greetings): Greetings
                }

                enum Greetings {
                    GOOD_MORNING
                }";

            // act
            // assert
            SchemaBuilder.New()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<Query>()
                .Create()
                .MakeExecutable()
                .Execute("{ hello(greetings: GOOD_MORNING) }")
                .ToJson()
                .MatchSnapshot();
        }

        [Fact]
        public void Declare_EnumType_With_Type_Extension()
        {
            // arrange
            var sdl =
                @"type Query {
                    hello(greetings: Greetings): Greetings
                }

                enum Greetings {
                    GOOD_MORNING
                }

                extend enum Greetings {
                    GOOD_EVENING
                }";

            // act
            // assert
            SchemaBuilder.New()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<Query>()
                .Create()
                .MakeExecutable()
                .Execute("{ hello(greetings: GOOD_EVENING) }")
                .ToJson()
                .MatchSnapshot();
        }

        [Fact]
        public async Task RequestBuilder_Declare_EnumType_With_Explicit_Value_Binding()
        {
            // arrange
            Snapshot.FullName();

            var sdl =
                @"type Query {
                    hello(greetings: Greetings): Greetings
                }

                enum Greetings {
                    GOOD @Bind(to: ""GoodMorning"")
                }";

            // act
            // assert
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<Query>()
                .ExecuteRequestAsync("{ hello(greetings: GOOD) }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task  RequestBuilder_Declare_EnumType_With_Implicit_Value_Binding()
        {
            // arrange
            Snapshot.FullName();

            var sdl =
                @"type Query {
                    hello(greetings: Greetings): Greetings
                }

                enum Greetings {
                    GOOD_MORNING
                }";

            // act
            // assert
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<Query>()
                .ExecuteRequestAsync("{ hello(greetings: GOOD_MORNING) }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task  RequestBuilder_Declare_EnumType_With_Type_Extension()
        {
            // arrange
            Snapshot.FullName();

            var sdl =
                @"type Query {
                    hello(greetings: Greetings): Greetings
                }

                enum Greetings {
                    GOOD_MORNING
                }

                extend enum Greetings {
                    GOOD_EVENING
                }";

            // act
            // assert
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<Query>()
                .ExecuteRequestAsync("{ hello(greetings: GOOD_EVENING) }")
                .MatchSnapshotAsync();
        }

        // https://github.com/ChilliCream/hotchocolate/issues/833
        [InlineData("GOOD_EVENING")]
        [InlineData("GOODEVENING")]
        [Theory]
        public async Task Try_Using_A_Enum_Value_That_Is_Not_Bound(string value)
        {
            // arrange
            Snapshot.FullName(new SnapshotNameExtension(value));

            var sdl =
                @"type Query {
                    hello(greetings: Greetings): Greetings
                }

                enum Greetings {
                    GOOD_MORNING
                }";

            // act
            // assert
           await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .AddResolver<Query>()
                .BindRuntimeType<Greetings>()
                .ExecuteRequestAsync($"{{ hello(greetings: \"{value}\") }}")
                .MatchSnapshotAsync();
        }

        public class Query
        {
            public Greetings Hello(Greetings greetings) => greetings;
        }

        public enum Greetings
        {
            GoodMorning,
            GoodEvening
        }
    }
}
