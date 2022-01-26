using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// A representation is a blob of data that is supposed to match
/// the combined requirements of the fields requested on an entity.
/// </summary>
public sealed class Representation
{
    /// <summary>
    /// Initializes a new instance of <see cref="Representation"/>.
    /// </summary>
    /// <param name="typeName">
    /// The type name of the entity.
    /// </param>
    /// <param name="data">
    /// The required data to resolve the data from an entity.
    /// </param>
    public Representation(NameString typeName, ObjectValueNode data)
    {
        TypeName = typeName.EnsureNotEmpty(nameof(typeName));
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Gets the type name of the entity.
    /// </summary>
    /// <value></value>
    public NameString TypeName { get; }

    /// <summary>
    /// Gets the required data to resolve the data from an entity.
    /// </summary>
    public ObjectValueNode Data { get; }
}
