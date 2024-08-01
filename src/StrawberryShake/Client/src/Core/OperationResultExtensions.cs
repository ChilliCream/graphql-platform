namespace StrawberryShake;

/// <summary>
/// This class provides extension methods for the operation result.
/// </summary>
public static class OperationResultExtensions
{
    /// <summary>
    /// Ensures that the operation result has no errors and throws a
    /// <see cref="GraphQLClientException"/> when the operation result
    /// has errors.
    /// </summary>
    /// <param name="result">The operation result.</param>
    /// <exception cref="ArgumentNullException">
    /// The operation result is null.
    /// </exception>
    /// <exception cref="GraphQLClientException">
    /// The operation result has errors.
    /// </exception>
    public static void EnsureNoErrors(this IOperationResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (result.Errors.Count > 0)
        {
            throw new GraphQLClientException(result.Errors);
        }
    }

    /// <summary>
    /// Indicates if the operation result has errors.
    /// </summary>
    /// <param name="result">The operation result.</param>
    /// <returns>
    /// <c>true</c>, if the result has errors; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The operation result is null.
    /// </exception>
    public static bool IsErrorResult(this IOperationResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.Errors.Count > 0;
    }

    /// <summary>
    /// Indicates if the operation result has errors.
    /// </summary>
    /// <param name="result">The operation result.</param>
    /// <returns>
    /// <c>true</c>, if the result has no errors; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The operation result is null.
    /// </exception>
    public static bool IsSuccessResult(this IOperationResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.Errors.Count == 0;
    }
}
