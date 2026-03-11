using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// This helper type is used to represent a <see cref="KeyValuePair{TKey, TValue}"/> as a GraphQL object type.
/// </summary>
internal sealed class KeyValuePairObjectType(IExtendedType runtimeType) : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        if (!runtimeType.IsGeneric
            || runtimeType.Definition != typeof(KeyValuePair<,>)
            || runtimeType.TypeArguments.Count != 2)
        {
            throw ThrowHelper.KeyValuePairType_InvalidRuntimeType(nameof(KeyValuePairObjectType));
        }

        ConfigureInternal((ObjectTypeDescriptor)descriptor);
    }

    private void ConfigureInternal(ObjectTypeDescriptor descriptor)
    {
        var descriptorExtension = descriptor.Extend();
        var context = descriptorExtension.Context;
        var configuration = descriptorExtension.Configuration;
        var keyType = runtimeType.TypeArguments[0];
        var valueType = runtimeType.TypeArguments[1];

        var typeName = NameFormattingHelpers.GetGraphQLName(runtimeType);
        configuration.Name = context.Naming.GetTypeName(typeName, TypeKind.Object);
        configuration.Description = context.Naming.GetTypeDescription(runtimeType.Type, TypeKind.Object);
        configuration.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        configuration.FieldBindingFlags = context.Options.DefaultFieldBindingFlags;
        configuration.FieldBindingType = runtimeType.Type;
        configuration.RuntimeType = runtimeType.Type;

        descriptor.InferFieldsFromFieldBindingType();
        descriptor.Field("key").Extend().Configuration.Type = TypeReference.Create(keyType, TypeContext.Output);
        descriptor.Field("value").Extend().Configuration.Type = TypeReference.Create(valueType, TypeContext.Output);
    }
}
