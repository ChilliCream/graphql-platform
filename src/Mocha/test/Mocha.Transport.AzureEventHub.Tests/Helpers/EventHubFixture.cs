using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Mocha.Transport.AzureEventHub.Tests.Helpers;

public sealed class EventHubFixture : IAsyncLifetime
{
    private const string EmulatorImage = "mcr.microsoft.com/azure-messaging/eventhubs-emulator:latest";
    private const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite:latest";
    private const string AzuriteAlias = "azurite";
    private const int EmulatorPort = 5672;

    private INetwork? _network;
    private IContainer? _azuriteContainer;
    private IContainer? _emulatorContainer;
    public string ConnectionString { get; private set; } = null!;

    public string GetHubForTest(string testCategory)
    {
        return testCategory switch
        {
            "send" => "test-hub-send",
            "pubsub" => "test-hub-pubsub",
            "reqreply" => "test-hub-reqreply",
            "batch" => "test-hub-batch",
            "fault" => "test-hub-fault",
            "concurrency" => "test-hub-concurrency",
            "headers" => "mocha.test-helpers.order-created",
            "partition" => "test-hub-partition",
            "recovery" => "test-hub-recovery",
            "inbox" => "test-hub-inbox",
            "middleware" => "test-hub-middleware",
            _ => throw new ArgumentException($"Unknown test category: {testCategory}")
        };
    }

    public string GetUniqueConsumerGroup()
    {
        // The emulator only supports pre-configured consumer groups.
        // $Default always exists and is sufficient for test isolation
        // when each test category has its own hub.
        return "$Default";
    }

    public async Task InitializeAsync()
    {
        var envConnectionString = Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING");

        if (!string.IsNullOrEmpty(envConnectionString))
        {
            ConnectionString = envConnectionString;
            return;
        }

        var configPath = Path.Combine(AppContext.BaseDirectory, "Config.json");

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                "Config.json not found in output directory. Ensure it is set to CopyToOutputDirectory.",
                configPath);
        }

        _network = new NetworkBuilder()
            .Build();

        await _network.CreateAsync();

        _azuriteContainer = new ContainerBuilder(AzuriteImage)
            .WithNetwork(_network)
            .WithNetworkAliases(AzuriteAlias)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Azurite"))
            .Build();

        await _azuriteContainer.StartAsync();

        _emulatorContainer = new ContainerBuilder(EmulatorImage)
            .WithNetwork(_network)
            .WithPortBinding(EmulatorPort, true)
            .WithResourceMapping(configPath, "/Eventhubs_Emulator/ConfigFiles/")
            .WithEnvironment("BLOB_SERVER", AzuriteAlias)
            .WithEnvironment("METADATA_SERVER", AzuriteAlias)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Emulator Service is Successfully Up!"))
            .Build();

        await _emulatorContainer.StartAsync();

        var host = _emulatorContainer.Hostname;
        var port = _emulatorContainer.GetMappedPublicPort(EmulatorPort);

        ConnectionString =
            "Endpoint=sb://" + host + ":" + port
            + ";SharedAccessKeyName=RootManageSharedAccessKey"
            + ";SharedAccessKey=SAS_KEY_VALUE"
            + ";UseDevelopmentEmulator=true";
    }

    public async Task DisposeAsync()
    {
        if (_emulatorContainer is not null)
        {
            await _emulatorContainer.DisposeAsync();
        }

        if (_azuriteContainer is not null)
        {
            await _azuriteContainer.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DisposeAsync();
        }
    }
}

[CollectionDefinition("EventHub")]
public class EventHubCollection : ICollectionFixture<EventHubFixture>;
