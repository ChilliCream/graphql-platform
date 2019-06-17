using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Client.Internal;

namespace HotChocolate.Client
{
    /// <summary>
    /// A connection for making HTTP requests against a GraphQL schema.
    /// </summary>
    public class Connection : IConnection
    {
        /// <summary>
        /// Creates a new connection instance used to make requests of the GitHub GraphQL API.
        /// </summary>
        /// <remarks>
        /// See more information regarding User-Agent requirements here: https://developer.github.com/v3/#user-agent-required.
        /// </remarks>
        /// <param name="productInformation">
        /// The name (and optionally version) of the product using this library, the name of your GitHub organization, or your GitHub username (in that order of preference). This is sent to the server as part of
        /// the user agent for analytics purposes, and used by GitHub to contact you if there are problems.
        /// </param>
        /// <param name="uri">
        /// The address to point this client to such as https://api.github.com or the URL to a GitHub Enterprise instance.
        /// </param>
        /// <param name="credentialStore">Provides credentials to the client when making requests.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> used to make requests.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="uri"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="productInformation"/>, <paramref name="uri"/>, <paramref name="credentialStore"/> or <paramref name="httpClient"/> are <see langword="null"/>.
        /// </exception>
        public Connection(HttpClient httpClient)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Gets the HTTP client for the connection.
        /// </summary>
        protected HttpClient HttpClient { get; }

        /// <inheritdoc />
        public virtual async Task<string> Run(IQueryRequest request, CancellationToken cancellationToken = default)
        {

            //using (var request = CreateRequest(token, query))
            {
                /*
                using (var response = await HttpClient.SendAsync(
                    request, cancellationToken)
                    .ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content
                        .ReadAsStringAsync()
                        .ConfigureAwait(false);
                }
                */
            }
            throw new NotImplementedException();
        }

            /*
        private HttpRequestMessage CreateRequest(string token, string query)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, );

            try
            {
                request.Content = new StringContent(query, Encoding.UTF8);
                return request;
            }
            catch (Exception)
            {
                request.Dispose();
                throw;
            }
        }

         */
    }
}
