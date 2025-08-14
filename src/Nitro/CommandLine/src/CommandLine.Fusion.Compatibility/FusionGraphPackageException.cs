namespace HotChocolate.Fusion;

/// <summary>
/// Represents an exception that is thrown when a fusion graph package is invalid.
/// </summary>
public class FusionGraphPackageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FusionGraphPackageException"/> class.
    /// </summary>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    public FusionGraphPackageException(string message) : base(message) { }
}
