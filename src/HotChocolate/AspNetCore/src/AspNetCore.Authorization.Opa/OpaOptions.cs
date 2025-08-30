using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The class representing OPA configuration options.
/// </summary>
public sealed class OpaOptions
{
    private readonly ConcurrentDictionary<string, Regex> _handlerKeysRegexes = new();

    public Uri BaseAddress { get; set; } = new("http://127.0.0.1:8181/v1/data/");

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(250);

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new();

    public Dictionary<string, ParseResult> PolicyResultHandlers { get; } = [];

    public Dictionary<string, OpaQueryRequestExtensionsHandler> OpaQueryRequestExtensionsHandlers { get; } = [];

    public OpaQueryRequestExtensionsHandler? GetOpaQueryRequestExtensionsHandler(string policyPath)
    {
        if (OpaQueryRequestExtensionsHandlers.Count == 0)
        {
            return null;
        }
        return OpaQueryRequestExtensionsHandlers.TryGetValue(policyPath, out var handler)
            ? handler :
            FindHandler(policyPath, OpaQueryRequestExtensionsHandlers);
    }

    public ParseResult GetPolicyResultParser(string policyPath)
    {
        if (PolicyResultHandlers.TryGetValue(policyPath, out var handler))
        {
            return handler;
        }
        handler = FindHandler(policyPath, PolicyResultHandlers);
        return handler ??
            throw new InvalidOperationException(
                $"No result handler found for policy: {policyPath}");
    }

    private THandler? FindHandler<THandler>(string policyPath, Dictionary<string, THandler> handlers)
    {
        var maybeHandler = handlers.SingleOrDefault(
            k =>
            {
                var regex = _handlerKeysRegexes.GetOrAdd(
                    k.Key,
                    new Regex(
                        k.Key,
                        RegexOptions.Compiled
                        | RegexOptions.Singleline
                        | RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(500)));
                return regex.IsMatch(policyPath);
            });

        return maybeHandler.Value;
    }
}

public delegate AuthorizeResult ParseResult(OpaQueryResponse response);

public delegate object? OpaQueryRequestExtensionsHandler(OpaAuthorizationHandlerContext context);
