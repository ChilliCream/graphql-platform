using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// Object type extensions are used to represent a type which has been extended
/// from some original type.
/// </para>
/// <para>
/// For example, this might be used to represent local data, or by a GraphQL service
/// which is itself an extension of another GraphQL service.
/// </para>
/// </summary>
public class ObjectTypeExtension : NamedTypeExtensionBase<ObjectTypeDefinition>
{
    private Action<IObjectTypeDescriptor>? _configure;

    /// <summary>
    /// Initializes a new  instance of <see cref="ObjectType"/>.
    /// </summary>
    protected ObjectTypeExtension()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new  instance of <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="configure">
    /// A delegate to specify the properties of this type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public ObjectTypeExtension(Action<IObjectTypeDescriptor> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Create a object type from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The object type definition that specifies the properties of the
    /// newly created object type.
    /// </param>
    /// <returns>
    /// Returns the newly created object type.
    /// </returns>
    public static ObjectTypeExtension CreateUnsafe(ObjectTypeDefinition definition)
        => new() { Definition = definition, };

    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Object;

    protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = ObjectTypeDescriptor.New(context.DescriptorContext);
                _configure!(descriptor);
                return descriptor.CreateDefinition();
            }

            return Definition;
        }
        finally
        {
            _configure = null;
        }
    }

    protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        ObjectTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);
        context.RegisterDependencies(definition);
    }

    protected override void Merge(
        ITypeCompletionContext context,
        INamedType type)
    {
        if (type is ObjectType objectType)
        {
            // we first assert that extension and type are mutable and by
            // this that they do have a type definition.
            AssertMutable();
            objectType.AssertMutable();

            ApplyGlobalFieldIgnores(
                Definition!,
                objectType.Definition!);

            Definition!.MergeInto(objectType.Definition!);
        }
        else
        {
            throw new ArgumentException(
                TypeResources.ObjectTypeExtension_CannotMerge,
                nameof(type));
        }
    }

    private void ApplyGlobalFieldIgnores(
        ObjectTypeDefinition extensionDef,
        ObjectTypeDefinition typeDef)
    {
        var fieldIgnores = extensionDef.GetFieldIgnores();

        if (fieldIgnores.Count > 0)
        {
            var fields = new List<ObjectFieldDefinition>();

            foreach (var binding in fieldIgnores)
            {
                switch (binding.Type)
                {
                    case ObjectFieldBindingType.Field:
                        if (typeDef.Fields.FirstOrDefault(
                            t => t.Name.EqualsOrdinal(binding.Name)) is { } f)
                        {
                            fields.Add(f);
                        }
                        break;

                    case ObjectFieldBindingType.Property:
                        if (typeDef.Fields.FirstOrDefault(
                            t => t.Member != null &&
                                binding.Name.EqualsOrdinal(t.Member.Name)) is { } p)
                        {
                            fields.Add(p);
                        }
                        break;
                }
            }

            foreach (var field in fields)
            {
                typeDef.Fields.Remove(field);
            }
        }
    }
}
