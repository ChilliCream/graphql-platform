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
    /// A connection for making HTTP requests against the GitHub GraphQL API endpoint.
    /// </summary>
    public class Connection : IConnection
    {
        private const string DefaultMediaType = "application/vnd.github.antiope-preview+json";

        /// <summary>
        /// Gets the address of the GitHub GraphQL API.
        /// </summary>
        public static Uri GithubApiUri { get; } = new Uri("https://api.github.com/graphql");

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
        /// <param name="token">The token to use to authenticate with the GitHub GraphQL API.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="productInformation"/> is <see langword="null"/>.
        /// </exception>
        public Connection(ProductHeaderValue productInformation, string token)
            : this(productInformation, GithubApiUri, token)
        {
        }

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
        /// <param name="token">The token to use to authenticate with the GitHub GraphQL API.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="uri"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="productInformation"/> or <paramref name="uri"/> are <see langword="null"/>.
        /// </exception>
        public Connection(ProductHeaderValue productInformation, Uri uri, string token)
            : this(productInformation, uri, new InMemoryCredentialStore(token))
        {
        }

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
        /// <param name="credentialStore">Provides credentials to the client when making requests.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="productInformation"/> or <paramref name="credentialStore"/> are <see langword="null"/>.
        /// </exception>
        public Connection(ProductHeaderValue productInformation, ICredentialStore credentialStore)
            : this(productInformation, GithubApiUri, credentialStore)
        {
        }

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
        /// <exception cref="ArgumentException">
        /// <paramref name="uri"/> is not an absolute URI.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="productInformation"/>, <paramref name="uri"/> or <paramref name="credentialStore"/> are <see langword="null"/>.
        /// </exception>
        public Connection(ProductHeaderValue productInformation, Uri uri, ICredentialStore credentialStore)
            : this(productInformation, uri, credentialStore, new HttpClient())
        {
        }

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
        /// <param name="credentialStore">Provides credentials to the client when making requests.</param>
        /// <param name="httpClient">An <see cref="HttpClient"/> used to make requests.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="productInformation"/>, <paramref name="credentialStore"/> or <paramref name="httpClient"/> are <see langword="null"/>.
        /// </exception>
        public Connection(ProductHeaderValue productInformation, ICredentialStore credentialStore, HttpClient httpClient)
            : this(productInformation, GithubApiUri, credentialStore, httpClient)
        {
        }

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
        public Connection(
            ProductHeaderValue productInformation,
            Uri uri,
            ICredentialStore credentialStore,
            HttpClient httpClient)
        {
            if (productInformation == null)
            {
                throw new ArgumentNullException(nameof(productInformation));
            }

            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            CredentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            if (!Uri.IsAbsoluteUri)
            {
                throw new ArgumentException("The base address for the connection must be an absolute URI.", nameof(uri));
            }

            Accept = new MediaTypeWithQualityHeaderValue(DefaultMediaType);
            UserAgent = new ProductInfoHeaderValue(productInformation.Name, productInformation.Version);
        }

        /// <inheritdoc />
        public Uri Uri { get; }

        /// <summary>
        /// Gets the credential store for the connection.
        /// </summary>
        protected ICredentialStore CredentialStore { get; }

        /// <summary>
        /// Gets the HTTP client for the connection.
        /// </summary>
        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Gets the Accept value for the connection.
        /// </summary>
        private MediaTypeWithQualityHeaderValue Accept { get; }

        /// <summary>
        /// Gets the User Agent value for the connection.
        /// </summary>
        private ProductInfoHeaderValue UserAgent { get; }

        /// <inheritdoc />
        public virtual async Task<string> Run(string query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var token = await CredentialStore.GetCredentials(cancellationToken).ConfigureAwait(false);

            using (var request = CreateRequest(token, query))
            {
                using (var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }

        private HttpRequestMessage CreateRequest(string token, string query)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Uri);

            try
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                request.Headers.Accept.Add(Accept);
                request.Headers.UserAgent.Add(UserAgent);

                request.Content = new StringContent(query, Encoding.UTF8);

                return request;
            }
            catch (Exception)
            {
                request.Dispose();
                throw;
            }
        }
    }
}
