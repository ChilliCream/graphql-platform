#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// The serialization exception is thrown whenever a type cannot
    /// serialize, deserialize or parse a value.
    /// </summary>
    public class SerializationException
        : GraphQLException
    {
        /// <summary>
        /// Initializes the <see cref="SerializationException"/>.
        /// </summary>
        /// <param name="message">
        /// The error message.
        /// </param>
        /// <param name="type">
        /// The type that caused the serialization exception.
        /// </param>
        /// <param name="path">
        /// The field path that points to the exact field causing the exception.
        /// </param>
        public SerializationException(string message, IType type, Path? path = null)
            : base(message)
        {
            Type = type;
            Path = path;
        }

        /// <summary>
        /// Initializes the <see cref="SerializationException"/>.
        /// </summary>
        /// <param name="error">
        /// The serialization error object.
        /// </param>
        /// <param name="type">
        /// The type that caused the serialization exception.
        /// </param>
        /// <param name="path">
        /// The field path that points to the exact field causing the exception.
        /// </param>
        public SerializationException(IError error, IType type, Path? path = null)
            : base(error)
        {
            Type = type;
            Path = path;
        }

        /// <summary>
        /// The type that caused the serialization exception.
        /// </summary>
        public IType Type { get; }

        /// <summary>
        /// The field path that points to the exact field causing the exception.
        /// </summary>
        public Path? Path { get; }
    }
}
