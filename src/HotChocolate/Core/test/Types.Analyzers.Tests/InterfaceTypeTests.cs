namespace HotChocolate.Types;

public class InterfaceTypeTests
{
    [Fact]
    public async Task GenerateSource_BatchResolver_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            public interface IUser
            {
                string Name { get; }
            }

            [InterfaceType<IUser>]
            public static partial class UserInterface
            {
                [BatchResolver]
                public static List<string> GetGreeting([Parent] List<IUser> users)
                    => default!;
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
