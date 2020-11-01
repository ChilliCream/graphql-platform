using System.Collections;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate
{
    /// <summary>
    /// Represents a abstract executable that is well known in the framework. If the execution
    /// engine encounters a <see cref="IExecutable"/>, it will call execute it
    /// </summary>
    public interface IExecutable
    {
        /// <summary>
        /// Executes the executable and returns a list
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the execution.
        /// </param>
        /// <returns>Returns a arbitrary list</returns>
        ValueTask<IList> ToListAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no
        /// elements.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the execution.
        /// </param>
        /// <returns>Returns the result</returns>
        ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the only element of a default value if no such element exists. This method
        /// throws an exception if more than one element satisfies the condition.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Prints the executable in its current state
        /// </summary>
        /// <returns>A string that represents the executables state</returns>
        string Print();
    }
}
