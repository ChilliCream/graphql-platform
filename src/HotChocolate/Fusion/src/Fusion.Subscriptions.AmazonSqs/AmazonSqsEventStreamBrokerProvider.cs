using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Subscriptions.AmazonSqs;

internal sealed class AmazonSqsEventStreamBrokerProvider : IEventStreamBrokerProvider
{
    private readonly AmazonSqsEventStreamOptions _options;

    public AmazonSqsEventStreamBrokerProvider(
        string name,
        IOptionsMonitor<AmazonSqsEventStreamOptions> options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Get(name);
        Validate(_options);
    }

    public IEventStreamBroker Create()
        => new AmazonSqsEventStreamBroker(_options);

    private static void Validate(AmazonSqsEventStreamOptions options)
    {
        if (options.ConfigureClient is null
            && string.IsNullOrWhiteSpace(options.ServiceUrl)
            && string.IsNullOrWhiteSpace(options.Region))
        {
            throw new InvalidOperationException(
                "Amazon SQS event stream broker options require a service URL, region, or client configuration.");
        }

        if (options.ConfigureClient is null
            && !string.IsNullOrWhiteSpace(options.ServiceUrl)
            && options.Credentials is null)
        {
            throw new InvalidOperationException(
                "Amazon SQS endpoint overrides require explicit credentials.");
        }

        if (options.WaitTimeSeconds is < 0 or > 20)
        {
            throw new InvalidOperationException(
                "Amazon SQS wait time must be between 0 and 20 seconds.");
        }

        if (options.MaxNumberOfMessages is < 1 or > 10)
        {
            throw new InvalidOperationException(
                "Amazon SQS maximum receive batch size must be between 1 and 10 messages.");
        }

        if (options.VisibilityTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Amazon SQS visibility timeout must be greater than zero seconds.");
        }

        if (string.IsNullOrWhiteSpace(options.QueueNamePrefix))
        {
            throw new InvalidOperationException(
                "Amazon SQS queue name prefix must not be empty.");
        }
    }
}
