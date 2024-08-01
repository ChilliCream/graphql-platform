namespace HotChocolate.Types;

/// <summary>
/// The error marker is to signal to the execution engine that the result is actually an error object that
/// is located in the internal execution state.
/// </summary>
public sealed class ErrorMarker
{
    private ErrorMarker()
    {
        // Intentionally left blank.
    }

    /// <summary>
    /// Gets the error marker instance.
    /// </summary>
    public static ErrorMarker Instance { get; } = new();
}
