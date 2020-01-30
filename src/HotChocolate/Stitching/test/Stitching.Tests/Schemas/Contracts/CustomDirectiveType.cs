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
            descriptor.Use(next => ctx =>
            {
                ctx.Result = ctx.Directive.GetArgument<DateTime>("d")
                    .ToUniversalTime();
                return next.Invoke(ctx);
            });
        }
    }
}
