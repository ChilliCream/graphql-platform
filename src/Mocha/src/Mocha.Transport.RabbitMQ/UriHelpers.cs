using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.WebUtilities;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Helper methods for extracting RabbitMQ-specific parameters from URIs.
/// </summary>
internal static class UriHelpers
{
    /// <summary>
    /// Attempts to extract a routing key from the URI query string parameter named "routingKey".
    /// </summary>
    /// <param name="uri">The URI to parse.</param>
    /// <param name="routingKey">When this method returns <c>true</c>, contains the decoded routing key value.</param>
    /// <returns><c>true</c> if a routing key was found in the query string; otherwise, <c>false</c>.</returns>
    public static bool TryGetRoutingKey(this Uri uri, [NotNullWhen(true)] out string? routingKey)
    {
        if (uri.Query is not "" and not null)
        {
            var enumerable = new QueryStringEnumerable(uri.Query);
            foreach (var value in enumerable)
            {
                if (value.EncodedName.Span is "routingKey")
                {
                    routingKey = new string(value.DecodeValue().Span);
                    return true;
                }
            }
        }
        routingKey = null;
        return false;
    }
}
