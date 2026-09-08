namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// The exception raised when the Rego data documents contributed by the FAR package and the
/// registered data providers cannot be merged, either because one of them is not a JSON object at
/// its root or because two of them define the same top-level key.
/// </summary>
internal sealed class RegoDataMergeException : Exception
{
    public RegoDataMergeException(string message)
        : base(message)
    {
    }

    public RegoDataMergeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
