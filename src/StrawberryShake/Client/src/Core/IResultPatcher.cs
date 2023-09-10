namespace StrawberryShake;

/// <summary>
/// The result patcher allows the operation executor to
/// handle result patches which are used when deferring data.
/// </summary>
/// <typeparam name="TData">
/// The data objects.
/// </typeparam>
public interface IResultPatcher<TData> where TData : class
{
    /// <summary>
    /// Sets the initial response on which patches can be applied.
    /// </summary>
    /// <param name="response">
    /// The response object.
    /// </param>
    void SetResponse(Response<TData> response);

    /// <summary>
    /// Patches the initial result and returns the patched response.
    /// </summary>
    /// <param name="response">
    /// The response object.
    /// </param>
    /// <returns>
    /// Returns the patched response.
    /// </returns>
    Response<TData> PatchResponse(Response<TData> response);
}
