using System;
using System.Text.Json;

namespace HotChocolate.Subscriptions;

/// <summary>
/// This is a helper to format the legacy topic objects to a base64 string.
/// </summary>
internal static class TopicFormatter
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static string Format<T>(T topic)
        => topic as string ??
            Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(topic, _options));
}
