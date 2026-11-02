using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion;

internal sealed class TestServerSession : IAsyncDisposable
{
    private readonly Channel<WebApplication> _cleanupPipeline = Channel.CreateUnbounded<WebApplication>();
    private bool _disposed;

    public TestServer CreateServer(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication,
        string? environmentName = null)
    {
        var builder = WebApplication.CreateEmptyBuilder(
            new WebApplicationOptions { EnvironmentName = environmentName });
        builder.WebHost.UseTestServer();
        configureServices(builder.Services);

        var app = builder.Build();
        configureApplication(app);

        app.Start();

        if (!_cleanupPipeline.Writer.TryWrite(app))
        {
            ((IHost)app).Dispose();
            throw new InvalidOperationException(
                "Failed to add test server to cleanup pipeline.");
        }

        return app.GetTestServer();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cleanupPipeline.Writer.TryComplete();

        while (_cleanupPipeline.Reader.TryRead(out var app))
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
