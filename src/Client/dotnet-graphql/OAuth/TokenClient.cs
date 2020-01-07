using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace StrawberryShake.Tools.OAuth
{
    public static class TokenClient
    {
        public static async Task<string> GetTokenAsync(
            string tokenEndpoint,
            string clientId,
            string clientSecret,
            IEnumerable<string> scopes,
            CancellationToken cancellationToken = default)
        {
            HttpClient client = new HttpClient();
            TokenResponse tokenRes = await client.RequestClientCredentialsTokenAsync(
                new ClientCredentialsTokenRequest
                {
                    Address = tokenEndpoint,
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = string.Join(" ", scopes)
                },
                cancellationToken).ConfigureAwait(false);
            return tokenRes.AccessToken;
        }
    }

    public class AccessToken
    {
        public AccessToken(string token, string scheme)
        {
            Token = token;
            Scheme = scheme;
        }

        public string Token { get; }

        public string Scheme { get; }
    }
}
