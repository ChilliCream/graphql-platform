using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for <see cref="OperationResult"/>.
/// </summary>
public static class ExecutionOperationResultExtensions
{
    extension(OperationResult result)
    {
        /// <summary>
        /// Unwraps the data from the operation result and returns the underlying <see cref="ResultElement"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="ResultElement"/> containing the operation result data.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the data object is not of type <see cref="ResultDocument"/>.
        /// </exception>
        public ResultElement UnwrapData()
        {
            if (result.Data?.Value is not ResultDocument resultDocument)
            {
                throw new InvalidOperationException(
                    "Unexpected data object type.");
            }

            return resultDocument.Data;
        }
    }
}
