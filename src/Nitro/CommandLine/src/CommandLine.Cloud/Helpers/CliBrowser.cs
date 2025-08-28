using Duende.IdentityModel.OidcClient.Browser;

namespace ChilliCream.Nitro.CommandLine.Cloud.Helpers;

internal class CliBrowser : IBrowser
{
    public async Task<BrowserResult> InvokeAsync(
        BrowserOptions options,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri(options.StartUrl);

        try
        {
            var result = await client.GetAsync(options.StartUrl, cancellationToken);

            if (result.IsSuccessStatusCode)
            {
                return new BrowserResult { Response = "", ResultType = BrowserResultType.Success };
            }

            return new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError,
                Error = "Empty response."
            };
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
}
