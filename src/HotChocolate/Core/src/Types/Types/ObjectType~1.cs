using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// GraphQL operations are hierarchical and composed, describing a tree of information.
/// While Scalar types describe the leaf values of these hierarchical operations,
/// Objects describe the intermediate levels.
/// </para>
/// <para>
/// GraphQL Objects represent a list of named fields, each of which yield a value of a
/// specific type. Object values should be serialized as ordered maps, where the selected
/// field names (or aliases) are the keys and the result of evaluating the field is the value,
/// ordered by the order in which they appear in the selection set.
/// </para>
/// <para>
/// All fields defined within an Object type must not have a name which begins
/// with "__" (two underscores), as this is used exclusively by
/// GraphQL’s introspection system.
/// </para>
/// </summary>
public class ObjectType<T> : ObjectType
{
    private Action<IObjectTypeDescriptor<T>>? _configure;

    /// <summary>
    /// Initializes a new  instance of <see cref="ObjectType{T}"/>.
    /// </summary>
    public ObjectType(Action<IObjectTypeDescriptor<T>> configure)
        => _configure = configure ?? throw new ArgumentNullException(nameof(configure));

    /// <summary>
    /// Initializes a new  instance of <see cref="ObjectType{T}"/>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ObjectType()
        => _configure = Configure;

    protected override ObjectTypeDefinition CreateDefinition(
        ITypeDiscoveryContext context)
    {
        var descriptor = ObjectTypeDescriptor.New<T>(context.DescriptorContext);

        _configure!(descriptor);
        _configure = null;

        return descriptor.CreateDefinition();
    }

    /// <summary>
    /// Override this to configure the type.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor allows to configure the interface type.
    /// </param>
    protected virtual void Configure(IObjectTypeDescriptor<T> descriptor) { }

    protected sealed override void Configure(IObjectTypeDescriptor descriptor)
        => throw new NotSupportedException();
}
