namespace StrawberryShake.Serialization
{
    /// <summary>
    /// The leaf value parser can parse a serialized leaf value to its runtime representation.
    /// </summary>
    /// <typeparam name="TSerialized">
    /// The type of the serialized value.
    /// </typeparam>
    /// <typeparam name="TRuntime">
    /// The type of the runtime value.
    /// </typeparam>
    public interface ILeafValueParser<in TSerialized, out TRuntime> : ISerializer
    {
        /// <summary>
        /// Parses the serialized value to the runtime representation.
        /// </summary>
        /// <param name="serializedValue">
        /// The serialized value.
        /// </param>
        /// <returns>
        /// Returns the parsed runtime representation.
        /// </returns>
        TRuntime Parse(TSerialized serializedValue);
    }
}
