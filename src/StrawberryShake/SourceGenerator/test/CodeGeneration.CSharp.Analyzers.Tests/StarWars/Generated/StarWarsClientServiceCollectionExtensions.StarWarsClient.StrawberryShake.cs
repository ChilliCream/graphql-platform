#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public static partial class StarWarsClientServiceCollectionExtensions
    {
        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddStarWarsClient(
            this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            global::StrawberryShake.ExecutionStrategy strategy = global::StrawberryShake.ExecutionStrategy.NetworkOnly)
        {
            var serviceCollection = new global::Microsoft.Extensions.DependencyInjection.ServiceCollection();

            ConfigureClient(
                serviceCollection,
                strategy);

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => new ClientServiceProvider(sp));

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<GetPeopleQuery>(
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ClientServiceProvider>(sp)));

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<StarWarsClient>(
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ClientServiceProvider>(sp)));

            return services;
        }

        private static global::Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureClient(
            global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            global::StrawberryShake.ExecutionStrategy strategy = global::StrawberryShake.ExecutionStrategy.NetworkOnly)
        {
            
            if (services is null)
            {
                throw new global::System.ArgumentNullException(nameof(services));
            }
            
            // register entity id factory
            
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId>>(services, EntityIdFactory.CreateEntityId);
            
            // register stores
            
            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<
                global::StrawberryShake.IEntityStore,
                global::StrawberryShake.EntityStore>(
                    services);
            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<
                global::StrawberryShake.IOperationStore>(
                    services,
                    sp => new global::StrawberryShake.OperationStore(
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<
                            global::StrawberryShake.IEntityStore
                            >(sp)
                        .Watch()
                        ));
            
            // register connections
            
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp =>
                {
                    var clientFactory =
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<
                            global::System.Net.Http.IHttpClientFactory
                            >(sp);
            
                    return new global::StrawberryShake.Transport.Http.HttpConnection(
                        () => clientFactory.CreateClient("StarWarsClient"));
                });
            
            // register mappers
            
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.IEntityMapper<PersonEntity, GetPeople_People_Nodes_Person>, GetPeople_People_Nodes_PersonFromPersonEntityMapper>(services);
            
            // register serializers
            
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.StringSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.BooleanSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.ByteSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.ShortSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.IntSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.LongSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.FloatSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.DecimalSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.UrlSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.UuidSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.IdSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.DateTimeSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.DateSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.ByteArraySerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.TimeSpanSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializerResolver, global::StrawberryShake.Serialization.SerializerResolver>(services);
            
            // register operations
            
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationResultDataFactory<IGetPeople>,
                GetPeopleFactory>(
                    services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, IGetPeople>,
                GetPeopleBuilder>(
                    services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationExecutor<IGetPeople>>(
                    services,
                    sp => new global::StrawberryShake.OperationExecutor<global::System.Text.Json.JsonDocument, IGetPeople>(
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.Transport.Http.HttpConnection>(sp),
                        () => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, IGetPeople>>(sp),
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IOperationStore>(sp),
                        strategy));
            
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<GetPeopleQuery>(services);
            
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<StarWarsClient>(services);
            
            return services;
        }

        private class ClientServiceProvider
            : System.IServiceProvider
            , System.IDisposable
        {
            private readonly System.IServiceProvider _provider;

            public ClientServiceProvider(System.IServiceProvider provider)
            {
                _provider = provider;
            }

            public object? GetService(System.Type serviceType)
            {
                return _provider.GetService(serviceType);
            }

            public void Dispose()
            {
                if (_provider is System.IDisposable d)
                {
                    d.Dispose();
                }
            }
        }
    }
}
