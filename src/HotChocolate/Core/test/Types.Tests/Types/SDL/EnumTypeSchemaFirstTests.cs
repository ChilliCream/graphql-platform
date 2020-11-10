using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors.Definitions;
using Snapshooter.Xunit;
using Xunit;

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


        public class Query
        {
            public Greetings Hello(Greetings greetings) => greetings;
        }

        public enum Greetings
        {
            GoodMorning,
            GoodEvening
        }

        public class SchemaFirstEnumTypeInterceptor : TypeInterceptor
        {
            public override void OnAfterInitialize(
                ITypeDiscoveryContext discoveryContext,
                DefinitionBase definition,
                IDictionary<string, object> contextData)
            {
                if (discoveryContext.Type is EnumType &&
                    definition is EnumTypeDefinition enumTypeDefinition &&
                    enumTypeDefinition.Name.Equals("Greetings"))
                {
                    enumTypeDefinition.RuntimeType = typeof(Greetings);
                    enumTypeDefinition.Values.First(t => t.Name.Equals("GOOD_MORNING")).Value = Greetings.GoodEvening;
                }
            }
        }
    }
}
