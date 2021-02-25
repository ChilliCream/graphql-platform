using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class SchemaGeneratorTests
    {
        [Fact]
        public void Schema_With_Spec_Errors()
        {
            AssertResult(
                @"
                    query getListingsCount {
                        listings {
                        ... ListingsPayload
                        }
                    }
                    fragment ListingsPayload on ListingsPayload{
                        count
                    }
                ",
                FileResource.Open("BridgeClientDemo.graphql"),
                "extend schema @key(fields: \"id\")");
        }
    }
}
