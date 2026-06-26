using System.Net;
using System.Net.Sockets;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

/// <summary>
/// Provides a real Azure Event Hubs endpoint for integration tests.
/// </summary>
/// <remarks>
/// The default path starts the Event Hubs emulator with Azurite. Set
/// AZURE_EVENTHUBS_CONNECTION_STRING and AZURE_EVENTHUBS_HUBS to use an existing namespace instead.
/// The existing namespace path is skipped when fewer than the required pre-created hubs are supplied.
/// </remarks>
public sealed class AzureEventHubsFixture : IAsyncLifetime
{
    private const int AmqpPort = 5672;
    private const int AzuriteBlobPort = 10000;
    private const string AzuriteAlias = "azurite";
    private const string ConfigContainerPath = "/Eventhubs_Emulator/ConfigFiles";

    private static readonly string[] s_defaultHubs =
    [
        "hub-a",
        "hub-b",
        "hub-c",
        "hub-d",
        "hub-e",
        "hub-f",
        "hub-g",
        "hub-h"
    ];

    private readonly INetwork? _network;
    private readonly IContainer? _azuriteContainer;
    private readonly IContainer? _eventHubsContainer;
    private readonly string? _configDirectory;
    private readonly string? _configPath;
    private readonly int _hostPort;
    private readonly bool _usesExistingNamespace;

    public AzureEventHubsFixture()
    {
        var connectionString = Environment.GetEnvironmentVariable("AZURE_EVENTHUBS_CONNECTION_STRING");
        var hubs = Environment.GetEnvironmentVariable("AZURE_EVENTHUBS_HUBS");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _usesExistingNamespace = true;
            ConnectionString = connectionString;
            Hubs = SplitHubs(hubs);
            ConsumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
            return;
        }

        _hostPort = GetFreeTcpPort();
        Hubs = s_defaultHubs;
        ConsumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
        ConnectionString =
            "Endpoint=sb://localhost:" + _hostPort
            + ";SharedAccessKeyName=RootManageSharedAccessKey"
            + ";SharedAccessKey=SAS_KEY_VALUE"
            + ";UseDevelopmentEmulator=true;";

        _configDirectory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "hotchocolate-eventhubs-emulator-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_configDirectory);
        _configPath = System.IO.Path.Combine(_configDirectory, "Config.json");
        File.WriteAllText(_configPath, CreateConfigJson(Hubs));

        _network = new NetworkBuilder()
            .WithName("eventhubs-" + Guid.NewGuid().ToString("N"))
            .Build();

        _azuriteContainer = new ContainerBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithNetwork(_network)
            .WithNetworkAliases(AzuriteAlias)
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(AzuriteBlobPort))
            .Build();

        _eventHubsContainer = new ContainerBuilder("mcr.microsoft.com/azure-messaging/eventhubs-emulator:latest")
            .WithNetwork(_network)
            .WithPortBinding(_hostPort, AmqpPort)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("BLOB_SERVER", AzuriteAlias)
            .WithEnvironment("METADATA_SERVER", AzuriteAlias)
            .WithResourceMapping(_configDirectory, ConfigContainerPath)
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilMessageIsLogged(
                    "Emulator Service is Successfully Up!"))
            .Build();
    }

    public string ConnectionString { get; private set; } = null!;

    public string ConsumerGroup { get; private set; } = null!;

    public IReadOnlyList<string> Hubs { get; private set; } = null!;

    public string SingleHub => GetHub(0);

    public string FanInHubA => GetHub(1);

    public string FanInHubB => GetHub(2);

    public string FanOutHub => GetHub(3);

    public string ResumeHub => GetHub(4);

    public string CancellationHub => GetHub(5);

    public string InvalidCursorHub => GetHub(6);

    public string GatewayHub => GetHub(7);

    public async ValueTask InitializeAsync()
    {
        if (_usesExistingNamespace)
        {
            if (Hubs.Count < s_defaultHubs.Length)
            {
                Assert.Skip(
                    "AZURE_EVENTHUBS_HUBS must provide at least eight pre-created hubs.");
            }

            return;
        }

        await _network!.CreateAsync();
        await _azuriteContainer!.StartAsync();
        await _eventHubsContainer!.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_eventHubsContainer is not null)
        {
            await _eventHubsContainer.DisposeAsync();
        }

        if (_azuriteContainer is not null)
        {
            await _azuriteContainer.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DisposeAsync();
        }

        if (_configDirectory is not null && Directory.Exists(_configDirectory))
        {
            Directory.Delete(_configDirectory, recursive: true);
        }
    }

    public async Task PublishAsync(
        string hub,
        string body,
        CancellationToken cancellationToken)
    {
        await using var producer = new EventHubProducerClient(
            ConnectionString,
            hub,
            CreateProducerOptions());
        await producer.SendAsync(
            [new EventData(BinaryData.FromString(body))],
            cancellationToken);
    }

    public async Task PublishToPartitionAsync(
        string hub,
        string partitionId,
        string body,
        CancellationToken cancellationToken)
    {
        await using var producer = new EventHubProducerClient(
            ConnectionString,
            hub,
            CreateProducerOptions());
        await producer.SendAsync(
            [new EventData(BinaryData.FromString(body))],
            new SendEventOptions { PartitionId = partitionId },
            cancellationToken);
    }

    private static EventHubProducerClientOptions CreateProducerOptions()
        => new()
        {
            RetryOptions = new EventHubsRetryOptions
            {
                MaximumRetries = 2,
                TryTimeout = TimeSpan.FromSeconds(10)
            }
        };

    private string GetHub(int index)
    {
        if (Hubs.Count <= index)
        {
            Assert.Skip(
                "The Azure Event Hubs fixture does not have enough pre-created hubs.");
        }

        return Hubs[index];
    }

    private static string[] SplitHubs(string? hubs)
        => string.IsNullOrWhiteSpace(hubs)
            ? []
            : hubs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string CreateConfigJson(IReadOnlyList<string> hubs)
    {
        var entities = string.Join(
            ",",
            hubs.Select(
                hub =>
                    $$"""
                    {
                      "Name": "{{hub}}",
                      "PartitionCount": "2",
                      "ConsumerGroups": []
                    }
                    """));

        return
            $$"""
            {
              "UserConfig": {
                "NamespaceConfig": [
                  {
                    "Type": "EventHub",
                    "Name": "emulatorNs1",
                    "Entities": [
                      {{entities}}
                    ]
                  }
                ],
                "LoggingConfig": {
                  "Type": "File"
                }
              }
            }
            """;
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
