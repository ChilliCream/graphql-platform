using HotChocolate.Types;

namespace HotChocolate.Execution.Integration.Directives;

public class ConstantDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Name("constant")
            .Location(DirectiveLocation.Object)
            .Location(DirectiveLocation.FieldDefinition)
            .Location(DirectiveLocation.Field);

        descriptor.Argument("value").Type<StringType>();

        descriptor.Use((next, directive) => context =>
        {
            context.Result = directive.GetArgumentValue<string>("value");
            return next(context);
        });
    }
}
