using StrawberryShake.Razor;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class RazorGeneratorTests
    {
        [Fact]
        public void Query_And_Mutation()
        {
            // force assembly to load!
            Assert.NotNull(typeof(QueryBase<>));

            AssertResult(
                settings: new() { RazorComponents = true },
                @"query GetBars($a: String! $b: String) {
                    bars(a: $a b: $b) {
                        id
                        name
                    }
                }

                mutation SaveBars($a: String! $b: String) {
                    saveBar(a: $a b: $b) {
                        id
                        name
                    }
                }",
                @"type Query {
                    bars(a: String!, b: String): [Bar]
                }

                type Mutation {
                    saveBar(a: String!, b: String): Bar
                }

                type Bar {
                    id: String!
                    name: String
                }",
                "extend schema @key(fields: \"id\")");
        }
    }
}
