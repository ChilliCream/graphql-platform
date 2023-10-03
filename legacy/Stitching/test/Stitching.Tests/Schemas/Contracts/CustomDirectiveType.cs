using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts;

public class CustomDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Name("custom");
        descriptor.Location(DirectiveLocation.Field);
        descriptor.Argument("d").Type<DateTimeType>();
        descriptor.Use((next, directive) => ctx =>
        {
            ctx.Result = directive.GetArgumentValue<DateTime>("d").ToUniversalTime();
            return next.Invoke(ctx);
        });
    }
}
