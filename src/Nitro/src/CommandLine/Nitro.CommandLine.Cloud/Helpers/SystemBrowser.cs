using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using static ChilliCream.Nitro.CLI.OidcConfiguration;

namespace ChilliCream.Nitro.CLI.Helpers;

internal class SystemBrowser(int? port = null, string? path = null) : IBrowser
{
    public int Port { get; } = port ?? GetRandomUnusedPort();

    public string Host => $"http://localhost:{Port}";

    public async Task<BrowserResult> InvokeAsync(
        BrowserOptions options,
        CancellationToken cancellationToken = default)
    {
        await using var listener = new LoopbackHttpListener(Port, path);

        Open(options.StartUrl);

        try
        {
            var result = await listener.WaitForCallbackAsync(cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(result))
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UnknownError,
                    Error = "Empty response."
                };
            }

            return new BrowserResult { Response = result, ResultType = BrowserResultType.Success };
        }
        catch (TaskCanceledException ex)
        {
            return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = ex.Message };
        }
        catch (Exception ex)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError,
                Error = ex.Message
            };
        }
    }

    public static void Open(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(
                    new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    private static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

file sealed class LoopbackHttpListener : IAsyncDisposable
{
    private const int DefaultTimeout = 60 * 5; // 5 mins (in seconds)

    private readonly IWebHost _host;
    private readonly TaskCompletionSource<string> _source = new();

    public LoopbackHttpListener(int port, string? path = null)
    {
        _host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(new UriBuilder("http", "localhost", port, path).ToString())
            .Configure(app => app.Run(Execute))
            .Build();

        _host.Start();
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Delay(500);
        if (!_source.Task.IsCompleted)
        {
            _source.SetCanceled();
        }

        _source.Task.Dispose();
        _host.Dispose();
    }

    private async Task Execute(HttpContext ctx)
    {
        switch (ctx.Request.Method)
        {
            case "GET":
                await SetResultAsync(ctx.Request.QueryString.Value!, ctx);
                break;

            case "POST" when !ctx.Request.ContentType!
                .Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase):

                ctx.Response.StatusCode = 415;

                break;

            case "POST":
            {
                using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8);
                var body = await sr.ReadToEndAsync();
                await SetResultAsync(body, ctx);

                break;
            }

            default:
                ctx.Response.StatusCode = 405;
                break;
        }
    }

    private async Task SetResultAsync(string value, HttpContext ctx)
    {
        try
        {
            ctx.Response.StatusCode = 302;
            ctx.Response.Headers.Location = $"{IdentityUrl}/Account/Close";
            await ctx.Response.Body.FlushAsync();

            _source.TrySetResult(value);
        }
        catch (Exception)
        {
            ctx.Response.StatusCode = 302;
            ctx.Response.Headers.Location = $"{IdentityUrl}/home/error?errorId=NitroClientError";
            await ctx.Response.Body.FlushAsync();
        }
    }

    public Task<string> WaitForCallbackAsync(
        int timeoutInSeconds = DefaultTimeout,
        CancellationToken cancellationToken = default)
        => _source.Task.WaitAsync(TimeSpan.FromSeconds(timeoutInSeconds), cancellationToken);
}
