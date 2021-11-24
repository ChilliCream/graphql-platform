using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Errors;

internal class ErrorObjectType<T> : ObjectType<T>
{
    private ITypeInspector? _typeInspector;

    protected sealed override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        _typeInspector = context.TypeInspector;
        return base.CreateDefinition(context);
    }

    protected override void Configure(IObjectTypeDescriptor<T> descriptor)
    {
        descriptor.Implements<ErrorInterfaceType>();

        descriptor.Extend().OnBeforeCreate(RewriteMessageFieldToNonNullableStringType);
    }

    private void RewriteMessageFieldToNonNullableStringType(ObjectTypeDefinition definition)
    {
        if (_typeInspector is null)
        {
            throw ThrowHelper.TypeInspectorCouldNotBeLoaded(this);
        }

        if (definition.Fields.FirstOrDefault(f => f.Name == "message") is not { } messageField)
        {
            throw ThrowHelper.MessageWasNotDefinedOnError(this, definition.RuntimeType);
        }

        messageField.Type = _typeInspector.GetTypeRef(typeof(NonNullType<StringType>));
    }
}
