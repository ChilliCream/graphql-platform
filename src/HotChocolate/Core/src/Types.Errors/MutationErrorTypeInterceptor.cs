namespace HotChocolate.Types;

internal sealed class MutationErrorTypeInterceptor<T>(T errorRegistrar)
    : TypeInterceptor
    where T : MutationErrorConfiguration
{
    internal override void OnBeforeCreateSchemaInternal(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder)
    {
        var b = (SchemaBuilder)schemaBuilder;
        foreach (var typeReference in errorRegistrar.OnResolveDependencies(context))
        {
            b.AddTypeReference(typeReference);
        }
    }

    public override IEnumerable<TypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        var context = discoveryContexts.First();
        return errorRegistrar.OnResolveDependencies(context.DescriptorContext);
    }

    public override void OnBeforeCompleteMutationField(
        ITypeCompletionContext completionContext,
        ObjectFieldDefinition mutationField)
        => errorRegistrar.OnConfigure(completionContext.DescriptorContext, mutationField);
}
