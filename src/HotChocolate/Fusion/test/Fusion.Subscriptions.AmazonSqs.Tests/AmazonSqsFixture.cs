using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Testcontainers.LocalStack;

namespace HotChocolate.Fusion.Subscriptions.AmazonSqs;

/// <summary>
/// Provides a real Amazon SQS endpoint for integration tests.
/// </summary>
/// <remarks>
/// The fixture starts LocalStack through the Testcontainers LocalStack module and creates queues
/// and topics at runtime through the AWS SDK clients.
/// </remarks>
public sealed class AmazonSqsFixture : IAsyncLifetime
{
    private const string QueueArnAttribute = "QueueArn";

    private readonly LocalStackContainer _container;

    public AmazonSqsFixture()
    {
        _container = new LocalStackBuilder("localstack/localstack:4.14.0").Build();
    }

    public string ServiceUrl { get; private set; } = null!;

    public string Region { get; } = "us-east-1";

    public AWSCredentials CreateCredentials()
        => new BasicAWSCredentials("test", "test");

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ServiceUrl = _container.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task PublishAsync(
        string queueUrl,
        string body,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient();

        await client.SendMessageAsync(
            new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = body
            },
            cancellationToken);
    }

    public async Task<string> CreateTopicAsync(
        string topic,
        CancellationToken cancellationToken)
    {
        using var client = CreateNotificationClient();
        var response = await client.CreateTopicAsync(topic, cancellationToken);
        return response.TopicArn;
    }

    public async Task PublishToTopicAsync(
        string topicArn,
        string body,
        CancellationToken cancellationToken)
    {
        using var client = CreateNotificationClient();

        await client.PublishAsync(
            new PublishRequest
            {
                TopicArn = topicArn,
                Message = body
            },
            cancellationToken);
    }

    public async Task<bool> QueueExistsAsync(
        string queueUrl,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient();

        try
        {
            await client.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = queueUrl,
                    AttributeNames = [QueueArnAttribute]
                },
                cancellationToken);

            return true;
        }
        catch (QueueDoesNotExistException)
        {
            return false;
        }
        catch (AmazonSQSException ex)
            when (ex.ErrorCode == "AWS.SimpleQueueService.NonExistentQueue")
        {
            return false;
        }
    }

    public async Task WaitForQueueDeletedAsync(
        string queueUrl,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!await QueueExistsAsync(queueUrl, cancellationToken))
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private AmazonSQSClient CreateClient()
        => new(
            CreateCredentials(),
            new AmazonSQSConfig
            {
                ServiceURL = ServiceUrl,
                AuthenticationRegion = Region
            });

    private AmazonSimpleNotificationServiceClient CreateNotificationClient()
        => new(
            CreateCredentials(),
            new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = ServiceUrl,
                AuthenticationRegion = Region
            });
}
