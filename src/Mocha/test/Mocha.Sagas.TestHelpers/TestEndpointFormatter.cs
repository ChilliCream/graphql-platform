using System.Text;
using Mocha;

namespace Mocha.Sagas.Tests;

// Note: IEndpointFormatter doesn't exist in this codebase - this is a stub for compatibility
public sealed class TestEndpointFormatter
{
    public string FormatName(Type type) => type.FullName ?? type.Name;

    public string GetQueueEndpoint(
        Type type,
        bool includeService = false,
        bool includeConsumer = false,
        bool isReply = false,
        bool isFault = false)
    {
        return GetQueueEndpoint(type.FullName ?? type.Name, includeService, includeConsumer, isReply, isFault);
    }

    public string GetQueueEndpoint(
        string queueName,
        bool includeService = false,
        bool includeConsumer = false,
        bool isReply = false,
        bool isFault = false)
    {
        var builder = new StringBuilder();
        builder.Append("queue://");
        builder.Append(queueName);
        builder.Append($"?includeService={includeService}");
        builder.Append($"&includeConsumer={includeConsumer}");
        builder.Append($"&isReply={isReply}");
        builder.Append($"&isFault={isFault}");
        return builder.ToString();
    }

    public string GetTopicEndpoint(Type type)
    {
        return $"topic://{type.FullName ?? type.Name}";
    }
}
