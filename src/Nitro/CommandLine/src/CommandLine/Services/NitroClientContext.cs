using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Services;

internal sealed class NitroClientContext : INitroClientContextProvider
{
    public Uri Url
    {
        get
        {
            return field ?? throw new InvalidOperationException($"{nameof(NitroClientContext)} hasn't been initialized.");
        }
        private set;
    }

    public INitroClientAuthorization? Authorization { get; private set; }

    public void Configure(string? apiUrl, INitroClientAuthorization? authorization)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            apiUrl = Constants.ApiUrl;
        }
        else if (!apiUrl.StartsWith("https://") && !apiUrl.StartsWith("http://"))
        {
            apiUrl = $"https://{apiUrl}";
        }

        var uriBuilder = new UriBuilder(apiUrl)
        {
            Path = "/graphql",
            Query = string.Empty,
            Fragment = string.Empty,
            UserName = string.Empty,
            Password = string.Empty
        };

        Url = uriBuilder.Uri;
        Authorization = authorization;
    }
}
