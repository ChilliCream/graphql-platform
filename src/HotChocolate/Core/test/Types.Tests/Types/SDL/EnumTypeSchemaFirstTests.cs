using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;
using System.Threading.Tasks;

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
                    GOOD
                }";

            // act
            // assert
            SchemaBuilder.New()
                .AddDocumentFromString(sdl)
                .BindComplexType<Query>()
                .BindEnumType<Greetings>(c => c.Value(Greetings.GoodMorning).To("GOOD"))
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
                .BindComplexType<Query>()
                .BindEnumType<Greetings>(c => c.Value(Greetings.GoodMorning))
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
                .BindComplexType<Query>()
                .BindEnumType<Greetings>(c => 
                {
                    c.Value(Greetings.GoodMorning);
                    c.Value(Greetings.GoodEvening);
                })
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
                    GOOD
                }";

            // act
            // assert
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindComplexType<Query>()
                .BindEnumType<Greetings>(c => c.Value(Greetings.GoodMorning).To("GOOD"))
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
                .BindComplexType<Query>()
                .BindEnumType<Greetings>(c => c.Value(Greetings.GoodMorning))
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
                .BindComplexType<Query>()
                .BindEnumType<Greetings>(c => 
                {
                    c.Value(Greetings.GoodMorning);
                    c.Value(Greetings.GoodEvening);
                })
                .ExecuteRequestAsync("{ hello(greetings: GOOD_EVENING) }")
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
