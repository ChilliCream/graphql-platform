using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal delegate Task<ConnectionPage<TItem>> FetchConnectionDataAsync<TItem>(
    string? after,
    int? first,
    CancellationToken cancellationToken = default);
