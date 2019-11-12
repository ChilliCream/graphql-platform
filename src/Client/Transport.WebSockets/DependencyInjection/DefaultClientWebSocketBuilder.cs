using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.WebSockets
{
    internal class DefaultClientWebSocketBuilder
        : IClientWebSocketBuilder
    {
        public DefaultClientWebSocketBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}
