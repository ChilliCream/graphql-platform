using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Serialization
{
    /// <summary>
    /// This interface specifies how a GraphQL result is serialized to a HTTP response.
    /// </summary>
    public interface IHttpResultSerializer
    {
        /// <summary>
        /// Gets the HTTP content type for the specified execution result.
        /// </summary>
        /// <param name="result">
        /// The GraphQL execution result.
        /// </param>
        /// <returns>
        /// Returns a string representing the content type,
        /// eg. "application/json; charset=utf-8".
        /// </returns>
        string GetContentType(IExecutionResult result);

        /// <summary>
        /// Gets the HTTP status code for the specified execution result.
        /// </summary>
        /// <param name="result">
        /// The GraphQL execution result.
        /// </param>
        /// <returns>
        /// Returns the HTTP status code, eg. <see cref="HttpStatusCode.OK"/>.
        /// </returns>
        HttpStatusCode GetStatusCode(IExecutionResult result);

        /// <summary>
        /// Serializes the specified execution result.
        /// </summary>
        /// <param name="result">
        /// The GraphQL execution result.
        /// </param>
        /// <param name="stream">
        /// The HTTP response stream.
        /// </param>
        /// <param name="cancellationToken">
        /// The request cancellation token.
        /// </param>
        ValueTask SerializeAsync(
            IExecutionResult result,
            Stream stream,
            CancellationToken cancellationToken);
    }
}
