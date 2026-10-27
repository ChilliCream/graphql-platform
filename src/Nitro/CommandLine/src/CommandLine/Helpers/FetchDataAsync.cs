namespace ChilliCream.Nitro.CommandLine.Helpers;

internal delegate Task<TResult> FetchDataAsync<TResult>(
    string? after,
    int? first,
    CancellationToken cancellationToken = default) where TResult : class;
