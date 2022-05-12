using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Stitching.Types.Strategies;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Types;

public class IgnoreTest
{
    [Fact]
    public async Task Test()
    {
        DocumentNode source = Utf8GraphQLParser.Parse(@"
            interface TestInterface @ignore {
              foo: [Test2!]
            }

            type Test implements TestInterface {
              id: String!
              foo: Test2!
            }

            type Test2 {
              test: Test!
            }

            type Test3 {
              foo: Test! @ignore
            }

            type Test4 {
              foo: TestInterface!
            }
",
            ParserOptions.NoLocation);

        var ignoreStrategy = new IgnoreStrategy();

        DocumentNode result = ignoreStrategy.Apply(source);

        var schema = result.Print();
        schema.MatchSnapshot();
    }
}
