using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate;

public interface ISchemaError
{
    /// <summary>
    /// Gets the error message.
    /// This property is mandatory and cannot be null.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets an error code that can be used to automatically
    /// process an error.
    /// This property is optional and can be null.
    /// </summary>
    string? Code { get; }

    ITypeSystemObject? TypeSystemObject { get; }

    IReadOnlyCollection<ISyntaxNode> SyntaxNodes { get; }

    /// <summary>
    /// Gets the path to the object that caused the error.
    /// This property is optional and can be null.
    /// </summary>
    IReadOnlyCollection<object>? Path { get; }

    /// <summary>
    /// Gets non-spec error properties.
    /// This property is optional and can be null.
    /// </summary>
    IReadOnlyDictionary<string, object> Extensions { get; }

    /// <summary>
    /// Gets the exception associated with this error.
    /// This property is optional and can be null.
    /// </summary>
    Exception? Exception { get; }
}
