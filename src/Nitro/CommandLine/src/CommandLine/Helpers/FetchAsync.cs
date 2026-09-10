using StrawberryShake;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal delegate Task<IOperationResult<TResult>> FetchAsync<TResult>(
    string? after,
    int? first,
    CancellationToken cancellationToken = default) where TResult : class;
