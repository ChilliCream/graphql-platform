using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace HotChocolate.Fusion.Subscriptions.AmazonSqs;

internal sealed class AmazonSqsEventStreamBroker(AmazonSqsEventStreamOptions options)
    : IEventStreamBroker
{
    private const string QueueArnAttribute = "QueueArn";
    private const string QueuePolicyAttribute = "Policy";
    private const string VisibilityTimeoutAttribute = "VisibilityTimeout";
    private const string MessageRetentionPeriodAttribute = "MessageRetentionPeriod";
    private const string RawMessageDeliveryAttribute = "RawMessageDelivery";

    private readonly IAmazonSQS _client = CreateQueueClient(options);
    private readonly IAmazonSimpleNotificationService? _notificationClient =
        options.ResolveTopicArn is null ? null : CreateNotificationClient(options);
    private readonly List<SubscriptionSession> _sessions = [];
    private bool _disposed;

    public IAsyncEnumerable<EventMessage> SubscribeAsync(
        ISubscriptionFieldContext context,
        string[] topics,
        string? cursor,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentOutOfRangeException.ThrowIfZero(topics.Length);

        for (var i = 0; i < topics.Length; i++)
        {
            ArgumentException.ThrowIfNullOrEmpty(topics[i]);
        }

        if (!string.IsNullOrEmpty(cursor))
        {
            throw new InvalidEventMessageCursorException();
        }

        return topics.Length == 1
            ? SubscribeSingleTopicAsync(topics[0], cancellationToken)
            : SubscribeMultipleTopicsAsync(topics, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        SubscriptionSession[] sessions;

        lock (_sessions)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            sessions = [.. _sessions];
            _sessions.Clear();
        }

        for (var i = 0; i < sessions.Length; i++)
        {
            sessions[i].Cancel();
        }

        for (var i = 0; i < sessions.Length; i++)
        {
            await DeleteSubscriptionsAsync(sessions[i].GetSubscriptionArns()).ConfigureAwait(false);
            await DeleteQueuesAsync(sessions[i].GetQueueUrls()).ConfigureAwait(false);
        }

        if (_client is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (_notificationClient is IDisposable notificationDisposable)
        {
            notificationDisposable.Dispose();
        }
    }

    private async IAsyncEnumerable<EventMessage> SubscribeSingleTopicAsync(
        string topic,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = CreateSession(cancellationToken);
        string? queueUrl = null;

        try
        {
            queueUrl = await CreateQueueAsync(topic, session, session.Token).ConfigureAwait(false);

            while (!session.Token.IsCancellationRequested)
            {
                ReceiveMessageResponse response;

                try
                {
                    response = await _client.ReceiveMessageAsync(
                        CreateReceiveRequest(queueUrl),
                        session.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (session.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex) when (session.Token.IsCancellationRequested
                    && ex is ObjectDisposedException or AmazonSQSException)
                {
                    break;
                }

                if (response.Messages is not { Count: > 0 } messages)
                {
                    continue;
                }

                for (var i = 0; i < messages.Count; i++)
                {
                    var message = messages[i];
                    var eventMessage = CreateMessage(message);

                    try
                    {
                        yield return eventMessage;
                    }
                    finally
                    {
                        await DeleteMessageAsync(queueUrl, message, session.Token).ConfigureAwait(false);
                    }
                }
            }
        }
        finally
        {
            session.Cancel();
            await DeleteSubscriptionsAsync(session.GetSubscriptionArns()).ConfigureAwait(false);
            await DeleteQueuesAsync(session.GetQueueUrls()).ConfigureAwait(false);
            RemoveSession(session);
            session.Dispose();
        }
    }

    private async IAsyncEnumerable<EventMessage> SubscribeMultipleTopicsAsync(
        string[] topics,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = options.CreateMessageChannel();
        var session = CreateSession(cancellationToken);
        var queueUrls = new string?[topics.Length];
        var pumpTasks = new Task[topics.Length];

        try
        {
            for (var i = 0; i < topics.Length; i++)
            {
                queueUrls[i] = await CreateQueueAsync(topics[i], session, session.Token)
                    .ConfigureAwait(false);
                pumpTasks[i] = PumpMessagesAsync(queueUrls[i]!, channel.Writer, session.Token);
            }

            await foreach (var message in ReadMessagesAsync(channel.Reader, session.Token)
                .ConfigureAwait(false))
            {
                yield return message;
            }
        }
        finally
        {
            session.Cancel();
            await WaitForPumpsAsync(pumpTasks).ConfigureAwait(false);
            await DeleteSubscriptionsAsync(session.GetSubscriptionArns()).ConfigureAwait(false);
            await DeleteQueuesAsync(session.GetQueueUrls()).ConfigureAwait(false);
            RemoveSession(session);
            channel.Writer.TryComplete();
            DisposeQueuedMessages(channel);
            session.Dispose();
        }
    }

    private SubscriptionSession CreateSession(CancellationToken cancellationToken)
    {
        var session = new SubscriptionSession(cancellationToken);

        lock (_sessions)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _sessions.Add(session);
        }

        return session;
    }

    private void RemoveSession(SubscriptionSession session)
    {
        lock (_sessions)
        {
            _sessions.Remove(session);
        }
    }

    private async Task<string> CreateQueueAsync(
        string topic,
        SubscriptionSession session,
        CancellationToken cancellationToken)
    {
        var response = await _client.CreateQueueAsync(
            new CreateQueueRequest
            {
                QueueName = CreateQueueName(topic, options.QueueNamePrefix),
                Attributes = new Dictionary<string, string>
                {
                    [VisibilityTimeoutAttribute] =
                        options.VisibilityTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                    [MessageRetentionPeriodAttribute] = "3600"
                }
            },
            cancellationToken)
            .ConfigureAwait(false);

        session.AddQueueUrl(response.QueueUrl);

        await SubscribeQueueToTopicAsync(topic, response.QueueUrl, session, cancellationToken)
            .ConfigureAwait(false);

        options.OnQueueReady?.Invoke(response.QueueUrl);

        return response.QueueUrl;
    }

    private async Task SubscribeQueueToTopicAsync(
        string topic,
        string queueUrl,
        SubscriptionSession session,
        CancellationToken cancellationToken)
    {
        if (options.ResolveTopicArn is not { } resolveTopicArn)
        {
            return;
        }

        var topicArn = resolveTopicArn(topic);

        if (string.IsNullOrWhiteSpace(topicArn))
        {
            throw new InvalidOperationException(
                "Amazon SQS SNS fan-out requires the topic ARN resolver to return a topic ARN.");
        }

        var queueArn = await GetQueueArnAsync(queueUrl, cancellationToken).ConfigureAwait(false);

        await SetQueuePolicyAsync(queueUrl, queueArn, topicArn, cancellationToken)
            .ConfigureAwait(false);

        var response = await _notificationClient!.SubscribeAsync(
            new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = "sqs",
                Endpoint = queueArn,
                Attributes = new Dictionary<string, string>
                {
                    [RawMessageDeliveryAttribute] = "true"
                }
            },
            cancellationToken)
            .ConfigureAwait(false);

        session.AddSubscriptionArn(response.SubscriptionArn);
    }

    private async Task<string> GetQueueArnAsync(
        string queueUrl,
        CancellationToken cancellationToken)
    {
        var response = await _client.GetQueueAttributesAsync(
            new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = [QueueArnAttribute]
            },
            cancellationToken)
            .ConfigureAwait(false);

        if (!response.Attributes.TryGetValue(QueueArnAttribute, out var queueArn)
            || string.IsNullOrWhiteSpace(queueArn))
        {
            throw new InvalidOperationException(
                "The Amazon SQS queue ARN could not be resolved.");
        }

        return queueArn;
    }

    private async Task SetQueuePolicyAsync(
        string queueUrl,
        string queueArn,
        string topicArn,
        CancellationToken cancellationToken)
    {
        await _client.SetQueueAttributesAsync(
            new SetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                Attributes = new Dictionary<string, string>
                {
                    [QueuePolicyAttribute] = CreateSnsQueuePolicy(queueArn, topicArn)
                }
            },
            cancellationToken)
            .ConfigureAwait(false);
    }

    private ReceiveMessageRequest CreateReceiveRequest(string queueUrl)
        => new()
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = options.MaxNumberOfMessages,
            WaitTimeSeconds = options.WaitTimeSeconds,
            VisibilityTimeout = options.VisibilityTimeoutSeconds,
            MessageAttributeNames = ["All"]
        };

    private async Task PumpMessagesAsync(
        string queueUrl,
        ChannelWriter<EventMessage> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var response = await _client.ReceiveMessageAsync(
                    CreateReceiveRequest(queueUrl),
                    cancellationToken)
                    .ConfigureAwait(false);

                if (response.Messages is not { Count: > 0 } messages)
                {
                    continue;
                }

                var accepted = new List<Message>(messages.Count);

                for (var i = 0; i < messages.Count; i++)
                {
                    var eventMessage = CreateMessage(messages[i]);

                    if (!await WriteMessageAsync(writer, eventMessage, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        break;
                    }

                    accepted.Add(messages[i]);
                }

                if (accepted.Count > 0)
                {
                    await DeleteMessagesAsync(queueUrl, accepted, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ChannelClosedException)
        {
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested
            && ex is ObjectDisposedException or AmazonSQSException)
        {
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
        }
    }

    private async Task DeleteMessageAsync(
        string queueUrl,
        Message message,
        CancellationToken cancellationToken)
    {
        try
        {
            await DeleteMessagesAsync(queueUrl, [message], CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (cancellationToken.IsCancellationRequested
            && ex is ObjectDisposedException or AmazonSQSException)
        {
        }
    }

    private async Task DeleteMessagesAsync(
        string queueUrl,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken)
    {
        var entries = new List<DeleteMessageBatchRequestEntry>(messages.Count);

        for (var i = 0; i < messages.Count; i++)
        {
            if (string.IsNullOrEmpty(messages[i].ReceiptHandle))
            {
                continue;
            }

            entries.Add(
                new DeleteMessageBatchRequestEntry(
                    i.ToString(CultureInfo.InvariantCulture),
                    messages[i].ReceiptHandle));
        }

        if (entries.Count == 0)
        {
            return;
        }

        var response = await _client.DeleteMessageBatchAsync(
            new DeleteMessageBatchRequest(queueUrl, entries),
            cancellationToken)
            .ConfigureAwait(false);

        if (response.Failed is { Count: > 0 })
        {
            throw new InvalidOperationException(
                "One or more Amazon SQS messages could not be deleted.");
        }
    }

    private async Task DeleteQueuesAsync(IEnumerable<string?> queueUrls)
    {
        foreach (var queueUrl in queueUrls)
        {
            await DeleteQueueAsync(queueUrl).ConfigureAwait(false);
        }
    }

    private async Task DeleteSubscriptionsAsync(IEnumerable<string?> subscriptionArns)
    {
        if (_notificationClient is null)
        {
            return;
        }

        foreach (var subscriptionArn in subscriptionArns)
        {
            await DeleteSubscriptionAsync(subscriptionArn).ConfigureAwait(false);
        }
    }

    private async Task DeleteSubscriptionAsync(string? subscriptionArn)
    {
        if (string.IsNullOrEmpty(subscriptionArn) || subscriptionArn == "PendingConfirmation")
        {
            return;
        }

        try
        {
            await _notificationClient!.UnsubscribeAsync(subscriptionArn, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (AmazonSimpleNotificationServiceException)
        {
        }
    }

    private async Task DeleteQueueAsync(string? queueUrl)
    {
        if (string.IsNullOrEmpty(queueUrl))
        {
            return;
        }

        try
        {
            await _client.DeleteQueueAsync(queueUrl, CancellationToken.None).ConfigureAwait(false);
        }
        catch (QueueDoesNotExistException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (AmazonSQSException)
        {
        }
    }

    private static async ValueTask<bool> WriteMessageAsync(
        ChannelWriter<EventMessage> writer,
        EventMessage eventMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            if (writer.TryWrite(eventMessage))
            {
                return true;
            }

            await writer.WriteAsync(eventMessage, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            eventMessage.Dispose();
            return false;
        }
        catch (ChannelClosedException)
        {
            eventMessage.Dispose();
            return false;
        }
    }

    private static async IAsyncEnumerable<EventMessage> ReadMessagesAsync(
        ChannelReader<EventMessage> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            EventMessage? message;

            try
            {
                if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    break;
                }

                if (!reader.TryRead(out message))
                {
                    continue;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            yield return message;
        }
    }

    private static async Task WaitForPumpsAsync(Task[] pumpTasks)
    {
        for (var i = 0; i < pumpTasks.Length; i++)
        {
            if (pumpTasks[i] is null)
            {
                continue;
            }

            try
            {
                await pumpTasks[i].ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (ex is ObjectDisposedException or AmazonSQSException)
            {
            }
        }
    }

    private static EventMessage CreateMessage(Message message)
    {
        var body = Encoding.UTF8.GetBytes(message.Body ?? string.Empty);
        return CreateMessage(body);
    }

    private static EventMessage CreateMessage(ReadOnlySpan<byte> body)
    {
        var owner = MemoryPool<byte>.Shared.Rent(body.Length);
        body.CopyTo(owner.Memory.Span);

        return new EventMessage(owner, 0..body.Length, 0..0);
    }

    private static string CreateQueueName(string topic, string prefix)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(topic)).AsSpan(0, 8))
            .ToLowerInvariant();
        var normalizedPrefix = NormalizeQueueNamePrefix(prefix);

        if (normalizedPrefix.Length > 30)
        {
            normalizedPrefix = normalizedPrefix[..30];
        }

        return normalizedPrefix + "-" + hash + "-" + Guid.NewGuid().ToString("N");
    }

    private static string NormalizeQueueNamePrefix(string prefix)
    {
        var builder = new StringBuilder(prefix.Length);

        for (var i = 0; i < prefix.Length; i++)
        {
            var c = prefix[i];

            if (char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            {
                builder.Append(c);
            }
            else
            {
                builder.Append('-');
            }
        }

        var value = builder.ToString().Trim('-');
        return value.Length == 0 ? "fusion-sub" : value;
    }

    private static string CreateSnsQueuePolicy(string queueArn, string topicArn)
    {
        var escapedQueueArn = EscapeJsonString(queueArn);
        var escapedTopicArn = EscapeJsonString(topicArn);

        return "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\","
            + "\"Principal\":{\"Service\":\"sns.amazonaws.com\"},"
            + "\"Action\":\"sqs:SendMessage\","
            + "\"Resource\":\"" + escapedQueueArn + "\","
            + "\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"" + escapedTopicArn + "\"}}}]}";
    }

    private static string EscapeJsonString(string value)
    {
        var builder = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append("\\\\");
                    break;

                case '\b':
                    builder.Append("\\b");
                    break;

                case '\f':
                    builder.Append("\\f");
                    break;

                case '\n':
                    builder.Append("\\n");
                    break;

                case '\r':
                    builder.Append("\\r");
                    break;

                case '\t':
                    builder.Append("\\t");
                    break;

                default:
                    if (c < ' ')
                    {
                        builder.Append("\\u");
                        builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    private static void DisposeQueuedMessages(Channel<EventMessage> channel)
    {
        while (channel.Reader.TryRead(out var message))
        {
            message.Dispose();
        }
    }

    private static IAmazonSQS CreateQueueClient(AmazonSqsEventStreamOptions options)
    {
        var config = options.ConfigureClient?.Invoke() ?? CreateQueueClientConfig(options);

        if (config is null)
        {
            throw new InvalidOperationException(
                "Amazon SQS client configuration callback must return a configuration instance.");
        }

        return options.Credentials is { } credentials
            ? new AmazonSQSClient(credentials, config)
            : new AmazonSQSClient(config);
    }

    private static IAmazonSimpleNotificationService CreateNotificationClient(
        AmazonSqsEventStreamOptions options)
    {
        var config = options.ConfigureNotificationClient?.Invoke()
            ?? CreateNotificationClientConfig(options);

        if (config is null)
        {
            throw new InvalidOperationException(
                "Amazon SNS client configuration callback must return a configuration instance.");
        }

        return options.Credentials is { } credentials
            ? new AmazonSimpleNotificationServiceClient(credentials, config)
            : new AmazonSimpleNotificationServiceClient(config);
    }

    private static AmazonSQSConfig CreateQueueClientConfig(AmazonSqsEventStreamOptions options)
    {
        var config = new AmazonSQSConfig();

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;

            if (!string.IsNullOrWhiteSpace(options.Region))
            {
                config.AuthenticationRegion = options.Region;
            }

            return config;
        }

        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        return config;
    }

    private static AmazonSimpleNotificationServiceConfig CreateNotificationClientConfig(
        AmazonSqsEventStreamOptions options)
    {
        var config = new AmazonSimpleNotificationServiceConfig();

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;

            if (!string.IsNullOrWhiteSpace(options.Region))
            {
                config.AuthenticationRegion = options.Region;
            }

            return config;
        }

        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        return config;
    }

    private sealed class SubscriptionSession(CancellationToken cancellationToken) : IDisposable
    {
        private readonly CancellationTokenSource _cts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        private readonly List<string> _queueUrls = [];
        private readonly List<string> _subscriptionArns = [];

        public CancellationToken Token => _cts.Token;

        public void AddQueueUrl(string queueUrl)
        {
            lock (_queueUrls)
            {
                _queueUrls.Add(queueUrl);
            }
        }

        public string[] GetQueueUrls()
        {
            lock (_queueUrls)
            {
                return [.. _queueUrls];
            }
        }

        public void AddSubscriptionArn(string? subscriptionArn)
        {
            if (string.IsNullOrWhiteSpace(subscriptionArn))
            {
                return;
            }

            lock (_subscriptionArns)
            {
                _subscriptionArns.Add(subscriptionArn);
            }
        }

        public string[] GetSubscriptionArns()
        {
            lock (_subscriptionArns)
            {
                return [.. _subscriptionArns];
            }
        }

        public void Cancel()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
