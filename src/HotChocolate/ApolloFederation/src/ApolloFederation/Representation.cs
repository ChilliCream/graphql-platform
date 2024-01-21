using HotChocolate.Language;
using HotChocolate.Utilities;

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
    public Representation(string typeName, ObjectValueNode data)
    {
        ArgumentNullException.ThrowIfNull(data);
        TypeName = typeName.EnsureGraphQLName();
        Data = data;
    }

    /// <summary>
    /// Gets the type name of the entity.
    /// </summary>
    /// <value></value>
    public string TypeName { get; }

    /// <summary>
    /// Gets the required data to resolve the data from an entity.
    /// </summary>
    public ObjectValueNode Data { get; }
}
