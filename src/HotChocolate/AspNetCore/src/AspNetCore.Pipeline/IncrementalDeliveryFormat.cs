namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents supported incremental delivery format versions from the Accept header.
/// </summary>
public enum IncrementalDeliveryFormat
{
    /// <summary>
    /// No incremental delivery format was specified.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// Incremental delivery format version 0.1.
    /// </summary>
    Version_0_1,

    /// <summary>
    /// Incremental delivery format version 0.2.
    /// </summary>
    Version_0_2
}
