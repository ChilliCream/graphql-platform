using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Adapters.OpenApi;

public sealed class TestServerSession : IDisposable
{
    private readonly Channel<IHost> _cleanupPipeline = Channel.CreateUnbounded<IHost>();
    private bool _disposed;

    public TestServer CreateServer(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(configureApplication)
                    .ConfigureServices(configureServices);
            })
            .Start();

        if (!_cleanupPipeline.Writer.TryWrite(host))
        {
            host.Dispose();
            throw new InvalidOperationException(
                "Failed to add test server to cleanup pipeline.");
        }

        return host.GetTestServer();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cleanupPipeline.Writer.TryComplete();

        while (_cleanupPipeline.Reader.TryRead(out var host))
        {
            host.Dispose();
        }
    }
}
