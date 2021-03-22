using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class PersistedQueryGeneratorTests
    {
        [Fact]
        public void Simple_Custom_Scalar() =>
            AssertResult(
                new AssertSettings
                {
                    RequestStrategy = Descriptors.Operations.RequestStrategy.PersistedQuery
                },
                "query GetPerson { person { name email } }",
                "type Query { person: Person }",
                "type Person { name: String! email: Email }",
                "scalar Email",
                "extend schema @key(fields: \"id\")");
    }
}
