using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Adapters.OpenApi;

public sealed class TestServerSession : IDisposable
{
    private readonly Channel<WebApplication> _cleanupPipeline = Channel.CreateUnbounded<WebApplication>();
    private bool _disposed;

    public TestServer CreateServer(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var options = new WebApplicationOptions
        {
            ApplicationName = typeof(TestServerSession).Assembly.GetName().Name!
        };
        var builder = WebApplication.CreateSlimBuilder(options);
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cleanupPipeline.Writer.TryComplete();

        while (_cleanupPipeline.Reader.TryRead(out var app))
        {
            ((IHost)app).Dispose();
        }
    }
}
