using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

[ObjectType<Chapter>]
public static partial class ChapterNode
{
    [NodeResolver]
    public static async Task<Chapter?> GetChapterByIdAsync(
        ChapterId id,
        ChapterRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetChapterAsync(id.BookId, id.Number, cancellationToken);
}
