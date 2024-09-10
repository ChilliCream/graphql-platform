using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Subscriptions.Diagnostics;
using Xunit.Abstractions;

namespace HotChocolate.Tests;

public sealed class SubscriptionTestDiagnostics : SubscriptionDiagnosticEventsListener
{
    private readonly ITestOutputHelper _output;

    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public SubscriptionTestDiagnostics(ITestOutputHelper output)
        => _output = output;

    public override void Created(string topicName)
        => _output.WriteLine($"Created: {topicName}");

    public override void Connected(string topicName)
        => _output.WriteLine($"Connected: {topicName}");

    public override void Disconnected(string topicName)
        => _output.WriteLine($"Disconnected: {topicName}");

    public override void MessageProcessingError(string topicName, Exception error)
    {
        _output.WriteLine(
            $"Error: {topicName} {error.Message} " +
            $"{error.StackTrace} {error.GetType().FullName}");
    }

    public override void Received(string topicName, string serializedMessage)
        => _output.WriteLine($"Received: {topicName} {serializedMessage}");

    public override void WaitForMessages(string topicName)
        => _output.WriteLine($"WaitForMessages: {topicName}");

    public override void Dispatch<T>(
        string topicName,
        T message,
        int subscribers)
        => _output.WriteLine($"Dispatched: {topicName} {Serialize(message)} {subscribers}");

    public override void TrySubscribe(string topicName, int attempt)
        => _output.WriteLine($"TrySubscribe: {topicName} {attempt}");

    public override void SubscribeSuccess(string topicName)
        => _output.WriteLine($"Subscribe Successful: {topicName}");

    public override void SubscribeFailed(string topicName)
        => _output.WriteLine($"Subscribe Failed: {topicName}");

    public override void Unsubscribe(string topicName, int shard, int subscribers)
        => _output.WriteLine($"Unsubscribe: {topicName}/{shard} {subscribers}");

    public override void Close(string topicName)
        => _output.WriteLine($"Close: {topicName}");

    public override void Send<T>(string topicName, T message)
        => _output.WriteLine($"Send: {topicName} {Serialize(message)}");

    public override void ProviderInfo(string infoText)
        => _output.WriteLine($"Info: {infoText}");

    public override void ProviderTopicInfo(string topicName, string infoText)
        => _output.WriteLine($"Info: {infoText}");

    private string Serialize<T>(T obj)
        => JsonSerializer.Serialize(obj, _options);
}
