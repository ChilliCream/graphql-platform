namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

/// <summary>
/// Describes a type reference like the type of a member, parameter or the return type of a method
/// </summary>
public sealed class PropertyDescriptor
{
    public PropertyDescriptor(
        string name,
        string fieldName,
        ITypeDescriptor type,
        string? description = null,
        PropertyKind kind = PropertyKind.Field)
    {
        Name = name;
        FieldName = fieldName;
        Type = type;
        Description = description;
        Kind = kind;
    }

    /// <summary>
    /// The name of the property
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the GraphQL field name.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// The referenced type
    /// </summary>
    public ITypeDescriptor Type { get; }

    /// <summary>
    /// The description of the property
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Defines the kind of this property.
    /// </summary>
    public PropertyKind Kind { get; }
}
