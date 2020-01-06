using System;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Tools.SchemaRegistry
{
    public sealed class SchemaRegistryClientFactory
    {
        private readonly IServiceProvider _services;

        public SchemaRegistryClientFactory(Uri uri)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient("SchemaRegistry");
            serviceCollection.AddSchemaRegistryClient();
            _services = serviceCollection.BuildServiceProvider();
        }

        public ISchemaRegistryClient Create() =>
            _services.GetRequiredService<ISchemaRegistryClient>();
    }
}
