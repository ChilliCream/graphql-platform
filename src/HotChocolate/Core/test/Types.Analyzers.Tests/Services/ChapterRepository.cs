namespace HotChocolate.Types;

public sealed class ChapterRepository
{
    private readonly Dictionary<(int, int), Chapter> _chapters = new()
    {
        { (1, 1), new Chapter(1, "Chapter 1", 1) },
        { (1, 2), new Chapter(2, "Chapter 2", 1) },
        { (2, 3), new Chapter(3, "Chapter 3", 2) }
    };

    public Task<Chapter?> GetChapterAsync(int bookId, int chapterNumber, CancellationToken cancellationToken)
        => _chapters.TryGetValue((bookId, chapterNumber), out var chapter)
            ? Task.FromResult<Chapter?>(chapter)
            : Task.FromResult<Chapter?>(null);

    public Task<IEnumerable<Chapter>> GetChaptersAsync(int bookId, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<Chapter>>(_chapters.Values.Where(t => t.BookId == bookId).OrderBy(t => t.Number));

    public Task<Chapter> CreateChapterAsync(int bookId, string title, CancellationToken cancellationToken)
    {
        var chapter = new Chapter(_chapters.Count + 1, title, bookId);
        _chapters.Add((bookId, chapter.Number), chapter);
        return Task.FromResult(chapter);
    }
}
