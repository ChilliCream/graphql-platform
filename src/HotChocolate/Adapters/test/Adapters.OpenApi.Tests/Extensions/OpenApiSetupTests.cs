using HotChocolate.Adapters.OpenApi.Configuration;
using HotChocolate.Adapters.OpenApi.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Adapters.OpenApi.Extensions;

public sealed class OpenApiSetupTests
{
    [Fact]
    public void StorageFactory_Should_BeSet_When_AddOpenApiDefinitionStorageCalled()
    {
        // arrange
        var services = new ServiceCollection();
        var storage = new StubOpenApiDefinitionStorage();

        // act
        services.AddGraphQL().AddOpenApiDefinitionStorage(storage);

        var serviceProvider = services.BuildServiceProvider();
        var setup = serviceProvider
            .GetRequiredService<IOptionsMonitor<OpenApiSetup>>()
            .Get(ISchemaDefinition.DefaultName);

        // assert
        Assert.NotNull(setup.StorageFactory);
        Assert.Same(storage, setup.StorageFactory(serviceProvider));
    }

    [Fact]
    public void Names_Should_ReturnRegisteredNames_When_MultipleSchemasRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddGraphQL("alpha").AddOpenApi().AddOpenApiDefinitionStorage(new StubOpenApiDefinitionStorage());
        services.AddGraphQL("beta").AddOpenApi().AddOpenApiDefinitionStorage(new StubOpenApiDefinitionStorage());

        // act
        var manager = services.BuildServiceProvider().GetRequiredService<OpenApiManager>();

        // assert
        Assert.Equal(["alpha", "beta"], manager.Names.Order());
    }

    private sealed class StubOpenApiDefinitionStorage : IOpenApiDefinitionStorage
    {
        public ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IEnumerable<IOpenApiDefinition>>([]);

        public IDisposable Subscribe(IObserver<OpenApiDefinitionStorageEventArgs> observer)
            => EmptyDisposable.Instance;

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
