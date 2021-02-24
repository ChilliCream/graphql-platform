using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ScalarGeneratorTests
    {
        [Fact]
        public void Simple_Custom_Scalar()
        {
            AssertResult(
                "query GetPerson { person { name email } }",
                "type Query { person: Person }",
                "type Person { name: String! email: Email }",
                "scalar Email",
                "extend schema @key(fields: \"id\")");
        }
    }
}
