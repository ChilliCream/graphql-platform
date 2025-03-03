namespace HotChocolate.Types;

public class PagingTests
{
    [Fact]
    public async Task GenerateSource_BatchDataLoader_MatchesSnapshot()
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

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public sealed class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
                public int AuthorId { get; set; }
            }

            [QueryType]
            public static partial class BookPage
            {
                public static Task<HotChocolate.Types.Pagination.Connection<Author>> GetAuthorsAsync(
                    GreenDonut.Data.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }
}
