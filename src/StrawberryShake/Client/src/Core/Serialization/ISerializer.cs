namespace StrawberryShake.Serialization
{
    /// <summary>
    /// This abstract serializer interfaces is used by
    /// <see cref="ILeafValueParser{TSerialized,TRuntime}"/> and <see cref="IInputValueFormatter"/>
    /// to refer to serializers in an abstract way.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// The name of the GraphQL type that is handled by this serializer.
        /// </summary>
        string TypeName { get; }
    }

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

    /// <summary>
    /// The input value formatter serializes input values so that they can be send to the server.
    /// </summary>
    public interface IInputValueFormatter : ISerializer
    {
        /// <summary>
        /// Formats an input value for transport.
        /// </summary>
        /// <param name="runtimeValue">
        /// The runtime representation of an input value.
        /// </param>
        /// <returns>
        /// Return a serialized/formatted version of the input value.
        /// </returns>
        object Format(object runtimeValue);
    }
}
