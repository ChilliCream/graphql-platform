using System.Reactive.Subjects;
using BeforeGraphQLDotNet.Models;

namespace BeforeGraphQLDotNet.Services;

public interface IBookEventService
{
    IObservable<Book> BookAdded { get; }

    void PublishBookAdded(Book book);
}

// Singleton event service backed by an Rx ReplaySubject so subscribers
// receive recently added books even when they subscribe slightly late.
public sealed class BookEventService : IBookEventService
{
    private readonly ReplaySubject<Book> _bookAdded = new(bufferSize: 1);

    public IObservable<Book> BookAdded => _bookAdded;

    public void PublishBookAdded(Book book)
    {
        _bookAdded.OnNext(book);
    }
}
