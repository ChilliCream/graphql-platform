using HotChocolate.RateLimit;
using HotChocolate.Types;

namespace HotChocolate.AspNetCore.RateLimit
{
    public class LimitDirectiveType : DirectiveType<LimitDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<LimitDirective> descriptor)
        {
            descriptor
                .Argument(x => x.Policy)
                .Description(
                    "The name of the limit policy that determines " +
                    "rate limiting to the annotated resource.")
                .Type<NonNullType<StringType>>();

            descriptor
                .Name("limit")
                .Location(DirectiveLocation.Schema)
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.FieldDefinition)
                .Repeatable()
                .Use<LimitMiddleware>();
        }
    }
}
