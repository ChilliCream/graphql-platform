namespace StrawberryShake.Serialization
{
    /// <summary>
    /// The serializer resolver provides centralised access to the serializers.
    /// </summary>
    public interface ISerializerResolver
    {
        /// <summary>
        /// Gets a <see cref="ILeafValueParser{TSerialized,TRuntime}"/> by its GraphQL type name.
        /// </summary>
        /// <param name="typeName">The GraphQL type name for which a parser is needed.</param>
        /// <typeparam name="TSerialized">The serialized value type.</typeparam>
        /// <typeparam name="TRuntime">The runtime value type.</typeparam>
        /// <returns>
        /// Returns the <see cref="ILeafValueParser{TSerialized,TRuntime}"/> for the specified
        /// GraphQL type.
        /// </returns>
        ILeafValueParser<TSerialized, TRuntime> GetLeafValueParser<TSerialized, TRuntime>(
            string typeName);

        /// <summary>
        /// Gets a <see cref="IInputValueFormatter"/> from the registry
        /// </summary>
        /// <param name="typeName">The type name of the input value formatter</param>
        /// <returns>The input value formatter for <paramref name="typeName"/></returns>
        IInputValueFormatter GetInputValueFormatter(string typeName);
    }
}
