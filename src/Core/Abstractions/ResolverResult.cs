using System;

namespace HotChocolate
{
    /// <summary>
    /// A resolver result represents an error or a value that is returned by the
    /// field-resolver. This interface provides a way to path field errors to
    /// the execution engine without throwing QueryExceptions.
    /// </summary>
    public readonly struct ResolverResult<TValue>
        : IResolverResult<TValue>
    {
        private ResolverResult(string errorMessage)
        {
            Value = default;
            ErrorMessage = errorMessage
                ?? throw new ArgumentNullException(nameof(errorMessage));
            IsError = true;
        }

        private ResolverResult(TValue value)
        {
            Value = value;
            ErrorMessage = null;
            IsError = false;
        }

        /// <summary>
        /// The error message that shall be used to create a
        /// field error if the resolver result represents an error.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Defines if the resolver result instance represents
        /// an error <c>true</c> or a value <c>false</c>.
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// The resolver result value that shall be processed by the
        /// execution engine in case this resolver is not an error.
        /// </summary>
        public TValue Value { get; }

        object IResolverResult.Value => Value;

        /// <summary>
        /// Creates a field error resolver result.
        /// </summary>
        /// <param name="errorMessage">
        /// The error message.
        /// </param>
        /// <returns>
        /// Returns a field error resolver result.
        /// </returns>
        public static ResolverResult<TValue> CreateError(string errorMessage)
        {
            return new ResolverResult<TValue>(errorMessage);
        }

        /// <summary>
        /// Creates a value resolver result.
        /// </summary>
        /// <param name="value">
        /// The reolver result value.
        /// </param>
        /// <returns>
        /// Returns a value resolver result.
        /// </returns>
        public static ResolverResult<TValue> CreateValue(TValue value)
        {
            return new ResolverResult<TValue>(value);
        }
    }
}
