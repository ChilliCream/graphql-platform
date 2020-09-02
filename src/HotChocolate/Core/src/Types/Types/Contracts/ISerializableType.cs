#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// A serializable type can serialize its runtime value to the result value
    /// format and deserialize the result value format back to its runtime value.
    /// </summary>
    public interface ISerializableType : IType
    {
        /// <summary>
        /// Serializes a runtime value of this type to the result value format.
        /// </summary>
        /// <param name="runtimeValue">
        /// A runtime value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a result value representation of this type.
        /// </returns>
        /// <exception cref="SerializationException">
        /// Unable to serialize the given <paramref name="runtimeValue"/>.
        /// </exception>
        object? Serialize(object? runtimeValue);

        /// <summary>
        /// Deserializes a result value of this type to the runtime value format.
        /// </summary>
        /// <param name="resultValue">
        /// A result value representation of this type.
        /// </param>
        /// <returns>
        /// Returns a runtime value representation of this type.
        /// </returns>
        object? Deserialize(object? resultValue);

        bool TryDeserialize(object? resultValue, out object? runtimeValue);
    }
}
