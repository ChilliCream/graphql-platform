using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Utilities.Introspection
{
    public interface IIntrospectionClient
    {
        /// <summary>
        /// Downloads the schema information from a GraphQL server
        /// and writes it as GraphQL SDL to the given stream.
        /// </summary>
        /// <param name="client">
        /// The HttpClient that shall be used to execute the introspection query.
        /// </param>
        /// <param name="stream">
        /// The stream to which the schema shall be written to.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        Task DownloadSchemaAsync(
            HttpClient client,
            Stream stream,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads the schema information from a GraphQL server
        /// and returns the schema syntax tree.
        /// </summary>
        /// <param name="client">
        /// The HttpClient that shall be used to execute the introspection query.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>Returns a parsed GraphQL schema syntax tree.</returns>
        Task<DocumentNode> DownloadSchemaAsync(
            HttpClient client,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the supported GraphQL features from the server by doing an introspection query.
        /// </summary>
        /// <param name="client">
        /// The HttpClient that shall be used to execute the introspection query.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>Returns an object that indicates what features are supported.</returns>
        Task<ISchemaFeatures> GetSchemaFeaturesAsync(
            HttpClient client,
            CancellationToken cancellationToken = default);
    }
}
