using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class OpaOptions
{
    public Uri BaseAddress { get; set; } = new("http://127.0.0.1:8181/v1/data/");
    public int TimeoutMs { get; set; } = 250;
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new();
    public Dictionary<string, IPolicyResultHandler> PolicyResultHandlers { get; } = new();
    private readonly ConcurrentDictionary<string, Regex> _handlerKeysRegexes = new();

    public IPolicyResultHandler GetResultHandlerFor(string policyPath)
    {
        if (PolicyResultHandlers.TryGetValue(policyPath, out IPolicyResultHandler? handler))
        {
            return handler;
        }

        KeyValuePair<string, IPolicyResultHandler> maybeHandler = PolicyResultHandlers.SingleOrDefault(k =>
        {
            Regex regex = _handlerKeysRegexes.GetOrAdd(k.Key,
                new Regex(k.Key, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant));
            return regex.IsMatch(policyPath);
        });
        return maybeHandler.Value ?? throw new InvalidOperationException($"No result handler found for policy: {policyPath}");
    }
}
