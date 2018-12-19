namespace HotChocolate
{
    /// <summary>
    /// A resolver result represents an error or a value that is returned by the
    /// field resolver. This interface provides a way to path field errors to
    /// the execution engine without throwing QueryExceptions.
    /// </summary>
    public interface IResolverResult
    {
        /// <summary>
        /// The error message that shall be used to create a
        /// field error if the resolver result represents an error.
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// Defines if the resolver result instance represents
        /// an error <c>true</c> or a value <c>false</c>.
        /// </summary>
        bool IsError { get; }

        /// <summary>
        /// The resolver result value that shall be processed by the
        /// execution engine in case this resolver is not an error.
        /// </summary>
        object Value { get; }
    }

    /// <summary>
    /// A resolver result represents an error or a value that is returned by the
    /// field resolver. This interface provides a way to path field errors to
    /// the execution engine without throwing QueryExceptions.
    /// </summary>
    public interface IResolverResult<out TValue>
        : IResolverResult
    {
        /// <summary>
        /// The resolver result value that shall be processed by the
        /// execution engine in case this resolver is not an error.
        /// </summary>
        new TValue Value { get; }
    }
}
