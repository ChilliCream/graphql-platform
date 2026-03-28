using Microsoft.Extensions.DependencyInjection;

namespace ChilliCream.Nitro.Client;

public sealed class NitroApiClientOptions
{
    public Func<IServiceProvider, Uri>? ResolveBaseAddress { get; set; }

    public Func<IServiceProvider, NitroAuthHeader>? ResolveAuthHeader { get; set; }

    public Action<HttpClient>? ConfigureHttpClient { get; set; }

    public Action<IHttpClientBuilder>? ConfigureHttpClientBuilder { get; set; }

    internal void EnsureValid()
    {
        if (ResolveBaseAddress is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ResolveBaseAddress)} must be configured.");
        }

        if (ResolveAuthHeader is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ResolveAuthHeader)} must be configured.");
        }
    }
}
