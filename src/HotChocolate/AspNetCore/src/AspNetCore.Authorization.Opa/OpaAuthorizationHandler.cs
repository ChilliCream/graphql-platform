using System.Collections.Concurrent;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// An implementation that delegates auth to OPA (Open Policy Agent) REST API endpoint
/// </summary>
internal sealed class OpaAuthorizationHandler : IAuthorizationHandler
{
    private readonly IOpaService _opaService;
    private readonly IOpaQueryRequestFactory _requestFactory;
    private readonly OpaOptions _options;

    public OpaAuthorizationHandler(
        IOpaService opaService,
        IOpaQueryRequestFactory requestFactory,
        IOptions<OpaOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _opaService = opaService ?? throw new ArgumentNullException(nameof(opaService));
        _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken cancellationToken = default)
    {
        return await AuthorizeAsync(
            new OpaAuthorizationHandlerContext(context), [directive], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken cancellationToken = default)
    {
        return await AuthorizeAsync(
            new OpaAuthorizationHandlerContext(context), directives, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<AuthorizeResult> AuthorizeAsync(
        OpaAuthorizationHandlerContext authContext,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken ct)
    {
        if (directives.Count == 1)
        {
            return await AuthorizeAsync(authContext, directives[0], ct).ConfigureAwait(false);
        }

        var tasks = Partitioner.Create(directives)
            .GetPartitions(2)
            .Select(partition => ExecuteAsync(authContext, partition, AuthorizeAsync, ct))
            .ToArray();

        var first = await Task.WhenAny(tasks).ConfigureAwait(false);
        var firstResult = await first;

        if (firstResult is not AuthorizeResult.Allowed)
        {
            return firstResult;
        }

        foreach (var task in tasks)
        {
            var result = await task.ConfigureAwait(false);

            if (result is not AuthorizeResult.Allowed)
            {
                return result;
            }
        }

        return AuthorizeResult.Allowed;

        static async Task<AuthorizeResult> ExecuteAsync(
            OpaAuthorizationHandlerContext context,
            IEnumerator<AuthorizeDirective> partition,
            Authorize authorize,
            CancellationToken ct)
        {
            while (partition.MoveNext())
            {
                var directive = partition.Current;
                var result = await authorize(context, directive, ct).ConfigureAwait(false);

                if (result is not AuthorizeResult.Allowed)
                {
                    return result;
                }
            }

            return AuthorizeResult.Allowed;
        }
    }

    private async ValueTask<AuthorizeResult> AuthorizeAsync(
        OpaAuthorizationHandlerContext context,
        AuthorizeDirective directive,
        CancellationToken ct)
    {
        var request = _requestFactory.CreateRequest(context, directive);
        var path = directive.Policy ?? string.Empty;
        var response = await _opaService.QueryAsync(path, request, ct).ConfigureAwait(false);
        var parseResult = _options.GetPolicyResultParser(path);
        return parseResult(response);
    }

    private delegate ValueTask<AuthorizeResult> Authorize(
        OpaAuthorizationHandlerContext context,
        AuthorizeDirective directive,
        CancellationToken ct);
}
