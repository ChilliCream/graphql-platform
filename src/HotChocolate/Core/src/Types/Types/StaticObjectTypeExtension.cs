#nullable enable
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

/// <summary>
/// This helper class is used to allow static type extensions.
/// </summary>
internal sealed class StaticObjectTypeExtension : ObjectTypeExtension
{
    private readonly Type _staticExtType;

    public StaticObjectTypeExtension(Type staticExtType)
        => _staticExtType = staticExtType;

    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        var context = descriptor.Extend().Context;
        var definition = descriptor.Extend().Definition;

        // we are using the non-generic type extension class which would set nothing.
        definition.Name = context.Naming.GetTypeName(_staticExtType, TypeKind.Object);
        definition.Description = context.Naming.GetTypeDescription(_staticExtType, TypeKind.Object);
        definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        definition.FieldBindingFlags = context.Options.DefaultFieldBindingFlags;
        definition.FieldBindingType = _staticExtType;
        definition.IsExtension = true;

        // we set the static type as runtime type. Since this is not the actual runtime
        // type and is replaced by the actual runtime type of the GraphQL type
        // we do not run into any conflicts here.
        definition.RuntimeType = _staticExtType;

        // next we set the binding flags to only infer static members.
        definition.FieldBindingFlags = FieldBindingFlags.Static;

        // last we use an internal helper to force infer the GraphQL fields from the
        // field binding type which is at this moment the runtime type that we have
        // set above.
        ((ObjectTypeDescriptor)descriptor).InferFieldsFromFieldBindingType();
    }
}
