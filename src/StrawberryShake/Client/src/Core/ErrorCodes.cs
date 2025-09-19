namespace StrawberryShake;

internal static class ErrorCodes
{
    /// <summary>
    /// The runtime value is expected to be {runtimeType} for {scalarType}.
    /// </summary>
    public const string InvalidRuntimeType = "SS0007";

    /// <summary>
    /// Error while building the result.
    /// </summary>
    public const string InvalidResultDataStructure = "SS1000";
}
