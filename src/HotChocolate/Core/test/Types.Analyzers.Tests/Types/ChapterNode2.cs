namespace HotChocolate.Types;

[ObjectType<Chapter>]
public static partial class ChapterNode2
{
    public static string FooBar => "test";

    public static async Task<Book?> GetBookAsync(
        [HotChocolate.Parent] Chapter chapter,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBookAsync(chapter.BookId, cancellationToken);
}
