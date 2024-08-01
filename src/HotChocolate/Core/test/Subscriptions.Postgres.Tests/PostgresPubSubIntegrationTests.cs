using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit.Abstractions;

namespace HotChocolate.Subscriptions.Postgres;

public class PostgresPubSubIntegrationTests
    : SubscriptionIntegrationTestBase
    , IClassFixture<PostgreSqlResource>
    , IAsyncLifetime
{
    private readonly PostgreSqlResource _resource;
    private readonly string _dbName = "Db_" + Guid.NewGuid().ToString("N");

    public PostgresPubSubIntegrationTests(
        PostgreSqlResource resource,
        ITestOutputHelper output) : base(output)
    {
        _resource = resource;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => _resource.CreateDatabaseAsync(_dbName);

    [Fact]
    public override Task Subscribe_Infer_Topic()
        => base.Subscribe_Infer_Topic();

    [Fact]
    public override Task Subscribe_Static_Topic()
        => base.Subscribe_Static_Topic();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments()
        => base.Subscribe_Topic_With_Arguments();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments_2_Subscriber()
        => base.Subscribe_Topic_With_Arguments_2_Subscriber();

    [Fact]
    public override Task Subscribe_Topic_With_Arguments_2_Topics()
        => base.Subscribe_Topic_With_Arguments_2_Topics();

    [Fact]
    public override Task Subscribe_Topic_With_2_Arguments()
        => base.Subscribe_Topic_With_2_Arguments();

    [Fact]
    public override Task Subscribe_And_Complete_Topic()
        => base.Subscribe_And_Complete_Topic();

    [Fact]
    public override Task Subscribe_And_Complete_Topic_With_ValueTypeMessage()
        => base.Subscribe_And_Complete_Topic_With_ValueTypeMessage();

    protected override void ConfigurePubSub(IRequestExecutorBuilder graphqlBuilder)
    {
        // register services
        graphqlBuilder.Services.AddLogging();

        // register subscription provider
        graphqlBuilder.AddPostgresSubscriptions((_, options) =>
        {
            options.ConnectionFactory = _ => new(_resource.GetConnection(_dbName));
        });
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;
}
