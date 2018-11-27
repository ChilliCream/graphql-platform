namespace HotChocolate
{
    /// <summary>
    /// Represents a schema or query error.
    /// </summary>
    public interface IError
    {
        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value></value>
        string Message { get; }
    }
}
