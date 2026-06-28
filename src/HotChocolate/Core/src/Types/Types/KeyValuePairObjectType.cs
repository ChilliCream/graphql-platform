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
#pragma warning disable IL2075
        var keyProperty = runtimeType.Type.GetProperty("Key")!;
        var valueProperty = runtimeType.Type.GetProperty("Value")!;
#pragma warning restore IL2075
        var keyType = runtimeType.TypeArguments[0];
        var valueType = runtimeType.TypeArguments[1];

        var typeName = NameFormattingHelpers.GetGraphQLName(runtimeType);
        configuration.Name = context.Naming.GetTypeName(typeName, TypeKind.Object);
        configuration.Description = context.Naming.GetTypeDescription(runtimeType.Type, TypeKind.Object);
        configuration.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        configuration.FieldBindingFlags = context.Options.DefaultFieldBindingFlags;
        configuration.FieldBindingType = runtimeType.Type;
        configuration.RuntimeType = runtimeType.Type;

        descriptor
            .Field(keyProperty)
            .Name("key")
            .Extend()
            .Configuration.Type = TypeReference.Create(keyType, TypeContext.Output);
        descriptor
            .Field(valueProperty)
            .Name("value")
            .Extend()
            .Configuration.Type = TypeReference.Create(valueType, TypeContext.Output);
    }
}
