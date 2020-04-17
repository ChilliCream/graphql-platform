using System.Diagnostics;

namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Represents a dedicated options accessor to read the error handler
    /// configuration.
    /// </summary>
    public interface IErrorHandlerOptionsAccessor
    {
        /// <summary>
        /// Gets a value indicating whether the <c>GraphQL</c> errors should be
        /// extended with exception details. The default value is
        /// <see cref="Debugger.IsAttached"/>.
        /// </summary>
        bool IncludeExceptionDetails { get; }
    }
}
