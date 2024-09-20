using RequestStrategyGen = StrawberryShake.Tools.Configuration.RequestStrategy;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp;

public class PersistedOperationGeneratorTests
{
    [Fact]
    public void Simple_Custom_Scalar() =>
        AssertResult(
            new AssertSettings
            {
                RequestStrategy = RequestStrategyGen.PersistedOperation,
            },
            "query GetPerson { person { name email } }",
            "type Query { person: Person }",
            "type Person { name: String! email: Email }",
            "scalar Email",
            "extend schema @key(fields: \"id\")");
}
