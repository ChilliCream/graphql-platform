namespace HotChocolate.Types;

internal sealed class ErrorObjectType<T> : ObjectType<T>
{
    protected override void Configure(IObjectTypeDescriptor<T> descriptor)
    {
        descriptor.Extend().OnBeforeCreate(RewriteMessageFieldToNonNullableStringType);
        descriptor.Extend().Definition.ContextData.MarkAsError();
        descriptor.BindFieldsImplicitly();
    }

    private void RewriteMessageFieldToNonNullableStringType(
        IDescriptorContext context,
        ObjectTypeDefinition definition)
    {
        // if a user provides his/her own error interface we will not rewrite the message type
        // and the user is responsible for ensuring that type and interface align.
        if (context.ContextData.ContainsKey(ErrorContextDataKeys.ErrorType))
        {
            return;
        }

        // if the error interface is the standard error interface it must provide a message
        // filed.
        if (definition.Fields.FirstOrDefault(f => f.Name == "message") is not { } messageField)
        {
            throw ThrowHelper.MessageWasNotDefinedOnError(this, definition.RuntimeType);
        }

        // we will ensure that the error message type is correct.
        messageField.Type = TypeReference.Parse("String!");
    }
}
