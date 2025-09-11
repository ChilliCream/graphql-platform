using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

internal sealed class TestServerSession : IDisposable
{
    private readonly Channel<TestServer> _cleanupPipeline = Channel.CreateUnbounded<TestServer>();
    private bool _disposed;

    public TestServer CreateServer(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var builder = new WebHostBuilder()
            .Configure(configureApplication)
            .ConfigureServices(configureServices);

        var server = new TestServer(builder);

        if (!_cleanupPipeline.Writer.TryWrite(server))
        {
            server.Dispose();
            throw new InvalidOperationException(
                "Failed to add test server to cleanup pipeline.");
        }

        return server;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cleanupPipeline.Writer.TryComplete();

        while (_cleanupPipeline.Reader.TryRead(out var server))
        {
            server.Dispose();
        }
    }
}
