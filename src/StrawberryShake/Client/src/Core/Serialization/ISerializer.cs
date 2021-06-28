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
}
