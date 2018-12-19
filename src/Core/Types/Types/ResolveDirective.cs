namespace HotChocolate.Types
{
    public sealed class ResolveDirective
        : DirectiveType<Resolve>
    {
        protected override void Configure(IDirectiveTypeDescriptor<Resolve> descriptor)
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

    public sealed class Resolve { }
}
