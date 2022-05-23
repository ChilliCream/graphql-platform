using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Stitching.Types.Renaming;
using Snapshooter.Xunit;
using Xunit;

public class RenameTest
{
    private static readonly DocumentNode Source = Utf8GraphQLParser.Parse(@"
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
              foo: TestInterface! @rename(name: ""test3_foo_renamed"")
            }

            type Test4 {
              foo: TestInterface!
            }
",
        ParserOptions.NoLocation);

    [Fact]
    public async Task TestTypeRename()
    {
        var renameStrategy = new TypeRenameMiddleware(_ => new ValueTask());
        var context = new DefaultRewriteContext(Source);
        await renameStrategy.NextAsync(context);

        context.Document
            .Print()
            .MatchSnapshot();
    }
}
