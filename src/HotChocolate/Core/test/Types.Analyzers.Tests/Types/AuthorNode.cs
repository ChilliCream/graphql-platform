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

    public static async Task<string> GetSomeInfo(
        [Parent] Author author,
        ISomeInfoByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
        => await dataLoader.LoadAsync(author.Id, cancellationToken);

    public static string GetAdditionalInfo(
        [Parent] Author author,
        string someArg)
        => someArg;

    public static string GetAdditionalInfo1(
        [Parent] Author author,
        string someArg1,
        string someArg2)
        => someArg1 + someArg2;


    [Query]
    public static string QueryFieldCollocatedWithAuthor() => "hello";
}
