using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Tools.SchemaRegistry
{
    public sealed class SchemaRegistryClientFactory
    {
        private readonly IServiceProvider _services;

        public SchemaRegistryClientFactory(Uri uri, string? token, string? scheme)
        {
            var serviceCollection = new ServiceCollection();
            var builder = serviceCollection.AddHttpClient("SchemaRegistry");

            if (token is { } && scheme is { })
            {
                builder.ConfigureHttpClient(c =>
                {
                    c.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(scheme, token);
                    c.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue(
                            new ProductHeaderValue(
                                "StrawberryShake",
                                typeof(InitCommand).Assembly!.GetName()!.Version!.ToString())));
                });
            }

            serviceCollection.AddSchemaRegistryClient();
            _services = serviceCollection.BuildServiceProvider();
        }

        public ISchemaRegistryClient Create() =>
            _services.GetRequiredService<ISchemaRegistryClient>();
    }
}
