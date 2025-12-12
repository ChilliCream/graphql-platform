namespace HotChocolate.Types;

public class CollectionInferenceTests
{
    [Fact]
    public async Task Infer_Dictionary_As_List_Of_KeyValuePair()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                public static Task<Dictionary<int, string?>> GetStuffAsync(
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }
}
