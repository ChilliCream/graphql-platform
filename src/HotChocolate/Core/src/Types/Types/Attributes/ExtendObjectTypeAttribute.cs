using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.FieldBindingFlags;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Annotate classes which represent extensions to other object types.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ExtendObjectTypeAttribute
    : ObjectTypeDescriptorAttribute
    , ITypeAttribute
{
    private string? _name;

    public ExtendObjectTypeAttribute(string? name = null)
    {
        _name = name;
    }

    public ExtendObjectTypeAttribute(OperationType operationType)
    {
        _name = operationType.ToString();
    }

    public ExtendObjectTypeAttribute(Type extendsType)
    {
        ExtendsType = extendsType;
    }

    /// <summary>
    /// Gets the GraphQL type name to which this extension is bound to.
    /// </summary>
    public string? Name
    {
        get => _name;
        [Obsolete("Use the new constructor.")]
        set => _name = value;
    }

    /// <summary>
    /// Defines if this attribute is inherited. The default is <c>false</c>.
    /// </summary>
    public bool Inherited { get; set; }

    /// <summary>
    /// Defines that static members are included.
    /// </summary>
    public bool IncludeStaticMembers { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Object;

    bool ITypeAttribute.IsTypeExtension => true;

    /// <summary>
    /// Gets the .NET type to which this extension is bound to.
    /// If this is a base type or an interface the extension will bind to all types
    /// inheriting or implementing the type.
    /// </summary>
    public Type? ExtendsType { get; }

    /// <summary>
    /// Gets a set of field names that will be removed from the extended type.
    /// </summary>
    public string[]? IgnoreFields { get; set; }

    /// <summary>
    /// Gets a set of property names that will be removed from the extended type.
    /// </summary>
    public string[]? IgnoreProperties { get; set; }

    /// <summary>
    /// Applies the type extension configuration.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="descriptor">
    /// The object type descriptor.
    /// </param>
    /// <param name="type">
    /// The type to which this instance is annotated to.
    /// </param>
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
    {
        if (ExtendsType is not null)
        {
            descriptor.ExtendsType(ExtendsType);
        }

        if (!string.IsNullOrEmpty(Name))
        {
            descriptor.Name(Name);
        }

        var definition = descriptor.Extend().Definition;
        definition.Fields.BindingBehavior = BindingBehavior.Implicit;

        if (IncludeStaticMembers)
        {
            definition.FieldBindingFlags = Instance | Static;
        }

        if (IgnoreFields is not null)
        {
            descriptor.Extend().OnBeforeCreate(d =>
            {
                foreach (var fieldName in IgnoreFields)
                {
                    d.FieldIgnores.Add(new ObjectFieldBinding(
                        fieldName,
                        ObjectFieldBindingType.Field));
                }
            });
        }

        if (IgnoreProperties is not null)
        {
            descriptor.Extend().OnBeforeCreate(d =>
            {
                foreach (var fieldName in IgnoreProperties)
                {
                    d.FieldIgnores.Add(new ObjectFieldBinding(
                        fieldName,
                        ObjectFieldBindingType.Property));
                }
            });
        }
    }
}

/// <summary>
/// Annotate classes which represent extensions to other object types.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ExtendObjectTypeAttribute<T>
    : ObjectTypeDescriptorAttribute
    , ITypeAttribute
{
    public ExtendObjectTypeAttribute()
    {
        ExtendsType = typeof(T);
    }

    /// <summary>
    /// Defines if this attribute is inherited. The default is <c>false</c>.
    /// </summary>
    public bool Inherited { get; set; }

    TypeKind ITypeAttribute.Kind => TypeKind.Object;

    bool ITypeAttribute.IsTypeExtension => true;

    /// <summary>
    /// Gets the .NET type to which this extension is bound to.
    /// If this is a base type or an interface the extension will bind to all types
    /// inheriting or implementing the type.
    /// </summary>
    public Type? ExtendsType { get; }

    /// <summary>
    /// Gets a set of field names that will be removed from the extended type.
    /// </summary>
    public string[]? IgnoreFields { get; set; }

    /// <summary>
    /// Defines that static members are included.
    /// </summary>
    public bool IncludeStaticMembers { get; set; }

    /// <summary>
    /// Gets a set of property names that will be removed from the extended type.
    /// </summary>
    public string[]? IgnoreProperties { get; set; }

    /// <summary>
    /// Applies the type extension configuration.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="descriptor">
    /// The object type descriptor.
    /// </param>
    /// <param name="type">
    /// The type to which this instance is annotated to.
    /// </param>
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
    {
        if (ExtendsType is not null)
        {
            descriptor.ExtendsType(ExtendsType);
        }

        var definition = descriptor.Extend().Definition;
        definition.Fields.BindingBehavior = BindingBehavior.Implicit;

        if (IncludeStaticMembers)
        {
            descriptor.Extend().Definition.FieldBindingFlags = Instance | Static;
        }

        if (IgnoreFields is not null)
        {
            descriptor.Extend().OnBeforeCreate(d =>
            {
                foreach (var fieldName in IgnoreFields)
                {
                    d.FieldIgnores.Add(new ObjectFieldBinding(
                        fieldName,
                        ObjectFieldBindingType.Field));
                }
            });
        }

        if (IgnoreProperties is not null)
        {
            descriptor.Extend().OnBeforeCreate(d =>
            {
                foreach (var fieldName in IgnoreProperties)
                {
                    d.FieldIgnores.Add(new ObjectFieldBinding(
                        fieldName,
                        ObjectFieldBindingType.Property));
                }
            });
        }
    }
}
