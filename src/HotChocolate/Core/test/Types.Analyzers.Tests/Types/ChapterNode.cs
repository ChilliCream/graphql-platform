using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

[ObjectType<Chapter>]
public static partial class ChapterNode
{
    public static async Task<Book?> GetBookAsync(
        [Parent] Chapter chapter,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBookAsync(chapter.BookId, cancellationToken);

    [NodeResolver]
    public static async Task<Chapter?> GetChapterByIdAsync(
        ChapterId id,
        ChapterRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetChapterAsync(id.BookId, id.Number, cancellationToken);
}
