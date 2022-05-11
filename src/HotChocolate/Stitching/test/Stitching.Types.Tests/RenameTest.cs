using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Stitching.Types.Attempt2;
using Xunit;

namespace HotChocolate.Stitching.Types;

public class RenameTest
{
    [Fact]
    public async Task Test()
    {
        DocumentNode source = Utf8GraphQLParser.Parse(@"
            interface TestInterface @rename(name: ""TestInterface_renamed"") {
              foo: [Test2!] @rename(name: ""foo_renamed"")
            }

            type Test implements TestInterface @rename(name: ""test_renamed"") {
              id: String! @rename(name: ""id_renamed"")
              foo: Test2!
            }

            type Test2 @rename(name: ""test2_renamed"") {
              test: Test!
            }

            type Test3 {
              foo: TestInterface!
            }
",
            ParserOptions.NoLocation);

        var renameStrategy = new TypeRenameStrategy();
        var fieldRenameStrategy = new FieldRenameStrategy();

        DocumentNode result = renameStrategy.Apply(source);
        result = fieldRenameStrategy.Apply(result);

        var schema = result.Print();
    }
}
