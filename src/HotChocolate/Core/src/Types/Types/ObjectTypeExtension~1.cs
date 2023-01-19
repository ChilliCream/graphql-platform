using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

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
public class ObjectTypeExtension<T> : ObjectTypeExtension
{
    private Action<IObjectTypeDescriptor<T>>? _configure;

    /// <summary>
    /// Initializes a new  instance of <see cref="ObjectType{T}"/>.
    /// </summary>
    public ObjectTypeExtension()
    {
        _configure = Configure;
    }

    /// <summary>
    /// Initializes a new  instance of <see cref="ObjectTypeExtension{T}"/>.
    /// </summary>
    public ObjectTypeExtension(Action<IObjectTypeDescriptor<T>> configure)
    {
        _configure = configure
            ?? throw new ArgumentNullException(nameof(configure));
    }

    protected override ObjectTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        var descriptor = ObjectTypeDescriptor.NewExtension<T>(context.DescriptorContext);

        _configure!(descriptor);
        _configure = null;

        // if the object type is inferred from a runtime time we will bind fields implicitly
        // even if the schema building option are set to bind explicitly by default;
        // otherwise we would end up with types that have no fields.
        if (context.IsInferred)
        {
            descriptor.BindFieldsImplicitly();
        }

        return descriptor.CreateDefinition();
    }

    /// <summary>
    /// Override this to configure the type.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor allows to configure the interface type.
    /// </param>
    protected virtual void Configure(IObjectTypeDescriptor<T> descriptor)
    {
    }

    protected sealed override void Configure(IObjectTypeDescriptor descriptor)
    {
        throw new NotSupportedException();
    }
}
