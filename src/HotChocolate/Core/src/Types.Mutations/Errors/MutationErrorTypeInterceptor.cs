namespace HotChocolate.Types;

internal sealed class MutationErrorTypeInterceptor<T>(T errorRegistrar) 
    : TypeInterceptor
    where T : MutationErrorConfiguration
{
    public override void OnBeforeCompleteMutationField(
        ITypeCompletionContext completionContext, 
        ObjectFieldDefinition mutationField)
        => errorRegistrar.OnConfigure(completionContext.DescriptorContext, mutationField);
}