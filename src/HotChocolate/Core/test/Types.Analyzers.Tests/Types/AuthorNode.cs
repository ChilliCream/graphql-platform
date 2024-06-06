using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types;

[ObjectType<Author>]
public static partial class AuthorNode
{
    [UsePaging]
    public static async Task<IEnumerable<Book>> GetBooksAsync(
        [Parent] Author author,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBooksByAuthorAsync(author.Id, cancellationToken);


    [Query]
    public static string QueryFieldCollocatedWithAuthor() => "hello";
}
