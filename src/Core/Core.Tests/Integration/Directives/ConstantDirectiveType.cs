using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Integration.Directives
{
    public class ConstantDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("constant");
            descriptor.Argument("value").Type<StringType>();

            descriptor.Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.FieldDefinition)
                .Location(DirectiveLocation.Field);

            descriptor.Middleware(next => context =>
            {
                context.Result = context.Directive.GetArgument<string>("value");
                return next(context);
            });
        }
    }
}
