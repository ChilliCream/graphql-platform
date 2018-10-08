namespace HotChocolate.Types
{
    public sealed class ResolveDirective
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("resolve");
            descriptor.Middleware(next => async context =>
            {
                context.Result = await context.ResolveAsync<object>();
                await next.Invoke(context);
            });

            descriptor.Location(DirectiveLocation.Schema)
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.Interface)
                .Location(DirectiveLocation.FieldDefinition)
                .Location(DirectiveLocation.Field);
        }
    }
}
