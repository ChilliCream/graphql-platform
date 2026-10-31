using GreenDonut.Data;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Types;

[QueryType]
public static partial class PagedQueries
{
    public static Task<PageConnection<Publisher>> GetPublishersAsync(
        PagingArguments pagingArgs,
        CancellationToken cancellationToken)
    {
        var page = Page<Publisher>.Create(
            [new Publisher(1, "Acme Press"), new Publisher(2, "Globe Books")],
            hasNextPage: true,
            hasPreviousPage: false,
            static publisher => publisher.Id.ToString());

        return Task.FromResult(new PageConnection<Publisher>(page));
    }

    public static Task<StreamPageConnection<Magazine>> GetMagazinesAsync(
        PagingArguments pagingArgs,
        CancellationToken cancellationToken)
        => Task.FromResult(new StreamPageConnection<Magazine>(StreamPage<Magazine>.Empty));
}

public sealed record Publisher(int Id, string Name);

public sealed record Magazine(int Id, string Title);
