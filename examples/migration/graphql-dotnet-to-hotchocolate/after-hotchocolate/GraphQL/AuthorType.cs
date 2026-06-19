using AfterHotChocolate.Models;
using AfterHotChocolate.Services;

namespace AfterHotChocolate.GraphQL;

// Extends the Author POCO with its books navigation.
[ObjectType<Author>]
public static partial class AuthorType
{
    public static IReadOnlyList<Book> GetBooks(
        [Parent] Author author,
        BookDataStore store)
        => store.GetBooksByAuthorId(author.Id);
}
