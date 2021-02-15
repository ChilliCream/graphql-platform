namespace StrawberryShake.Serialization
{
    /// <summary>
    /// A input value formatter for input objects
    /// </summary>
    public interface IInputObjectFormatter : IInputValueFormatter
    {
        /// <summary>
        /// Initializes the serializer on the formatter
        /// </summary>
        /// <param name="serializerResolver">
        /// The <see cref="SerializerResolver"/> to lookup serializers
        /// </param>
        void Initialize(ISerializerResolver serializerResolver);
    }
}
