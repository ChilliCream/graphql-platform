using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Authorization;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class OpaOptions
{
    private readonly ConcurrentDictionary<string, Regex> _handlerKeysRegexes = new();

    public Uri BaseAddress { get; set; } = new("http://127.0.0.1:8181/v1/data/");

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(250);

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new();

    public Dictionary<string, ParseResult> PolicyResultHandlers { get; } = new();

    public ParseResult GetPolicyResultParser(string policyPath)
    {
        if (PolicyResultHandlers.TryGetValue(policyPath, out var handler))
        {
            return handler;
        }

        var maybeHandler = PolicyResultHandlers.SingleOrDefault(
            k =>
            {
                var regex = _handlerKeysRegexes.GetOrAdd(
                    k.Key,
                    new Regex(
                        k.Key,
                        RegexOptions.Compiled |
                        RegexOptions.Singleline |
                        RegexOptions.CultureInvariant));
                return regex.IsMatch(policyPath);
            });

        return maybeHandler.Value ??
            throw new InvalidOperationException(
                $"No result handler found for policy: {policyPath}");
    }
}

public delegate AuthorizeResult ParseResult(OpaQueryResponse response);
