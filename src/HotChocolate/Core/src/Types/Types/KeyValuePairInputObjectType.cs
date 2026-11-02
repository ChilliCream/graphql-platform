using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// This helper type is used to represent a <see cref="KeyValuePair{TKey, TValue}"/> as a GraphQL input object type.
/// </summary>
internal sealed class KeyValuePairInputObjectType(IExtendedType runtimeType) : InputObjectType
{
    protected override void Configure(IInputObjectTypeDescriptor descriptor)
    {
        if (!runtimeType.IsGeneric
            || runtimeType.Definition != typeof(KeyValuePair<,>)
            || runtimeType.TypeArguments.Count != 2)
        {
            throw ThrowHelper.KeyValuePairType_InvalidRuntimeType(nameof(KeyValuePairInputObjectType));
        }

        ConfigureInternal((InputObjectTypeDescriptor)descriptor);
    }

    private void ConfigureInternal(InputObjectTypeDescriptor descriptor)
    {
        var descriptorExtension = descriptor.Extend();
        var context = descriptorExtension.Context;
        var configuration = descriptorExtension.Configuration;
        var keyType = runtimeType.TypeArguments[0];
        var valueType = runtimeType.TypeArguments[1];

        var typeName = NameFormattingHelpers.GetGraphQLName(runtimeType);
        configuration.Name = context.Naming.GetTypeName(typeName, TypeKind.InputObject);
        configuration.Description = context.Naming.GetTypeDescription(runtimeType.Type, TypeKind.InputObject);
        configuration.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        configuration.RuntimeType = runtimeType.Type;

        descriptor.InferFieldsFromFieldBindingType();
        descriptor.Field("key").Extend().Configuration.Type = TypeReference.Create(keyType, TypeContext.Input);
        descriptor.Field("value").Extend().Configuration.Type = TypeReference.Create(valueType, TypeContext.Input);
    }
}
