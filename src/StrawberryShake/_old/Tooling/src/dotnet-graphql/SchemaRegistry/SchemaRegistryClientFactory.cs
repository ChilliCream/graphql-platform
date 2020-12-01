using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.Tools.SchemaRegistry
{
    public sealed class SchemaRegistryClientFactory
    {
        private readonly IServiceProvider _services;

        public SchemaRegistryClientFactory(Uri uri, string? token, string? scheme)
        {
            string schema = uri.Scheme == "https" ? "wss" : "ws";
            string path = uri.AbsolutePath == "/" ? string.Empty : uri.AbsolutePath;
            var socketUri = new Uri($"{schema}://{uri.Authority}{path}");

            var serviceCollection = new ServiceCollection();
            var builder = serviceCollection.AddHttpClient("SchemaRegistryClient")
                .ConfigureHttpClient(c => c.BaseAddress = uri);

            if (token is { } && scheme is { })
            {
                builder.ConfigureHttpClient(c =>
                {
                    c.BaseAddress = uri;
                    c.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(scheme, token);
                    c.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue(
                            new ProductHeaderValue(
                                "StrawberryShake",
                                typeof(InitCommand).Assembly!.GetName()!.Version!.ToString())));
                });
            }

            serviceCollection.AddWebSocketClient("SchemaRegistryClient")
                .ConfigureWebSocketClient(c => c.Uri = socketUri);

            serviceCollection.AddSchemaRegistryClient();
            _services = serviceCollection.BuildServiceProvider();
        }

        public ISchemaRegistryClient Create() =>
            _services.GetRequiredService<ISchemaRegistryClient>();
    }
}
