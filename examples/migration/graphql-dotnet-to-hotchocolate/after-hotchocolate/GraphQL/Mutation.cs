using AfterHotChocolate.Models;
using AfterHotChocolate.Services;
using HotChocolate.Subscriptions;

namespace AfterHotChocolate.GraphQL;

[MutationType]
public static partial class Mutation
{
    // With mutation conventions enabled this surfaces as
    // addBook(input: AddBookInput!): AddBookPayload! where AddBookPayload { book: Book }.
    public static async Task<Book> AddBook(
        string title,
        [ID] int authorId,
        BookGenre genre,
        int publishedYear,
        BookDataStore store,
        ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var book = store.AddBook(title, authorId, genre, publishedYear);

        await eventSender.SendAsync(
            nameof(Subscription.OnBookAdded),
            book,
            cancellationToken);

        return book;
    }
}
