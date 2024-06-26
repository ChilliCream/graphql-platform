using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types;

[ObjectType<Chapter>]
public static partial class ChapterNode2
{
    public static async Task<Book?> GetBookAsync(
        [Parent] Chapter chapter,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBookAsync(chapter.BookId, cancellationToken);
}
