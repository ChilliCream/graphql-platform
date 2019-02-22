using System;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class CustomDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("custom");
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Argument("d").Type<DateTimeType>();
            descriptor.Middleware(next => ctx =>
            {
                ctx.Result = ctx.Directive.GetArgument<DateTime>("d");
                return next.Invoke(ctx);
            });
        }
    }
}
