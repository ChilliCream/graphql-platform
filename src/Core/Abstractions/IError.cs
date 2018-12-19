using System.Collections.Generic;

namespace HotChocolate
{
    /// <summary>
    /// Represents a schema or query error.
    /// </summary>
    public interface IError
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
        string Code { get; }

        /// <summary>
        /// Gets the path to the object that caused the error.
        /// This property is optional and can be null.
        /// </summary>
        IReadOnlyCollection<string> Path { get; }

        /// <summary>
        /// Gets the source text positions to which this error refers to.
        /// This property is optional and can be null.
        /// </summary>
        IReadOnlyCollection<Location> Locations { get; }

        /// <summary>
        /// Gets non-spec error properties.
        /// This property is optional and can be null.
        /// </summary>
        IReadOnlyDictionary<string, object> Extensions { get; }
    }
}
