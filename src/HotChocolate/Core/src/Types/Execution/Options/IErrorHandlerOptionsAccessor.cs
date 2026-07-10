using System.Diagnostics;
using HotChocolate.Language;

namespace HotChocolate.Execution.Options;

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

    /// <summary>
    /// Gets the default error handling mode for null propagation.
    /// </summary>
    ErrorHandlingMode DefaultErrorHandlingMode { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="DefaultErrorHandlingMode"/> can be
    /// overridden on a per-request basis.
    /// </summary>
    bool AllowErrorHandlingModeOverride { get; }
}
