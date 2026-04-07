using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Services;

internal sealed class NitroClientContext : INitroClientContextProvider
{
    public Uri Url { get; private set; } = new Uri(Constants.ApiUrl);

    public INitroClientAuthorization? Authorization { get; private set; }

    public void Configure(string? apiUrl, INitroClientAuthorization? authorization)
    {
        Url = apiUrl is not null
            ? new Uri($"https://{apiUrl}/graphql")
            : new Uri(Constants.ApiUrl + "/graphql");
        Authorization = authorization;
    }
}
