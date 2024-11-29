namespace StrawberryShake.Serialization;

/// <summary>
/// This class defines how StrawberryShake deals with references on serialization and
/// deserialization of GraphQL types.
/// </summary>
public class SerializerResolver : ISerializerResolver
{
    private readonly Dictionary<string, ISerializer> _serializers = new();

    /// <summary>
    /// Initializes a new <see cref="SerializerResolver"/>
    /// </summary>
    /// <param name="serializers">
    /// A enumerable of <see cref="ISerializer"/> that shall be known to the resolver
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// In case <paramref name="serializers" /> is <c>null</c>
    /// </exception>
    public SerializerResolver(IEnumerable<ISerializer> serializers)
    {
        if (serializers is null)
        {
            throw new ArgumentNullException(nameof(serializers));
        }

        foreach (var serializer in serializers)
        {
            _serializers.TryAdd(serializer.TypeName, serializer);
        }

        foreach (var serializer in
            _serializers.Values.OfType<IInputObjectFormatter>())
        {
            serializer.Initialize(this);
        }
    }

    /// <summary>
    /// Resolves a <see cref="ILeafValueParser{TSerialized,TRuntime}"/> from the known
    /// serializers
    /// </summary>
    /// <param name="typeName">The GraphQL type name of the requested serializer</param>
    /// <typeparam name="TSerialized">The serialized value type</typeparam>
    /// <typeparam name="TRuntime">The runtime value type</typeparam>
    /// <returns>A <see cref="ILeafValueParser{TSerialized,TRuntime}"/></returns>
    /// <exception cref="ArgumentNullException">
    /// In case <paramref name="typeName"/> is null
    /// </exception>
    /// <exception cref="ArgumentException">
    /// In case the serializer was not found
    /// </exception>
    public ILeafValueParser<TSerialized, TRuntime> GetLeafValueParser<TSerialized, TRuntime>(
        string typeName)
    {
        if (typeName is null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        if (_serializers.TryGetValue(typeName, out var serializer) &&
            serializer is ILeafValueParser<TSerialized, TRuntime> parser)
        {
            return parser;
        }

        throw new ArgumentException("There is no parser registered the specified type.");
    }

    /// <summary>
    /// Resolves a <see cref="IInputValueFormatter"/> from the known serializers
    /// </summary>
    /// <param name="typeName">The GraphQL type name of the requested serializer</param>
    /// <returns>A <see cref="IInputValueFormatter"/></returns>
    /// <exception cref="ArgumentNullException">
    /// In case <paramref name="typeName"/> is null
    /// </exception>
    /// <exception cref="ArgumentException">
    /// In case the serializer was not found
    /// </exception>
    public IInputValueFormatter GetInputValueFormatter(string typeName)
    {
        if (typeName is null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        if (_serializers.TryGetValue(typeName, out var serializer) &&
            serializer is IInputValueFormatter formatter)
        {
            return formatter;
        }

        throw new ArgumentException("There is no formatter registered the specified type.");
    }
}
