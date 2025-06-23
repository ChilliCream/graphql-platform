namespace HotChocolate.Language;

/// <summary>
/// Represents an error that occurs when the operation id has an invalid format.
/// </summary>
public sealed class OperationIdFormatException : SyntaxException
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationIdFormatException"/>.
    /// </summary>
    /// <param name="reader">
    /// The reader that encountered the syntax error.
    /// </param>
    internal OperationIdFormatException(Utf8GraphQLReader reader)
        : base(reader, "The operation id has an invalid format.")
    {
    }
}
