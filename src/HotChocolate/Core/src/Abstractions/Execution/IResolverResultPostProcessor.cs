using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

/// <summary>
/// The result post processor allows to post process the result of the resolver pipeline
/// before the value goes through the GraphQL value completion.
///
/// This is useful if you want to complete IO operations like reading the content of a file streams
/// into memory so that it can go through the value completion or other async operations.
/// </summary>
public interface IResolverResultPostProcessor
{
    /// <summary>
    /// Processes the result of the resolver pipeline.
    /// </summary>
    /// <param name="result">
    /// The result of the resolver pipeline.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the processed result.
    /// </returns>
    ValueTask<object> ProcessResultAsync(
        object result,
        CancellationToken cancellationToken);
}
