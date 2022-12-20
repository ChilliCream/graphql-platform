namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// The type system member binding information.
/// </summary>
internal class MemberBinding
{
    /// <summary>
    /// Initializes a new instance of <see cref="MemberBinding"/>.
    /// </summary>
    /// <param name="schemaName">
    /// The schema to which the type system member is bound to.
    /// </param>
    /// <param name="name">
    /// The name which the type system member has in the <see cref="SchemaName"/>.
    /// </param>
    public MemberBinding(string schemaName, string name)
    {
        SchemaName = schemaName;
        Name = name;
    }

    /// <summary>
    /// Gets the schema to which the type system member is bound to.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the name which the type system member has in the <see cref="SchemaName"/>.
    /// </summary>
    public string Name { get; }
}
