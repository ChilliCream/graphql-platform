using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Authorization
{
    public sealed class AuthorizeDirectiveType
        : DirectiveType<AuthorizeDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<AuthorizeDirective> descriptor)
        {
            descriptor
                .Name("authorize")
                .Location(DirectiveLocation.Schema)
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.FieldDefinition)
                .Repeatable();

            descriptor.Argument(t => t.Policy)
                .Description(
                    "The name of the authorization policy that determines " +
                    "access to the annotated resource.")
                .Type<StringType>();

            descriptor.Argument(t => t.Roles)
                .Description(
                    "Roles that are allowed to access the " +
                    "annotated resource.")
                .Type<ListType<NonNullType<StringType>>>();

            descriptor.Argument(t => t.Apply)
                .Description(
                    "Defines when when the resolver shall be executed." +
                    "By default the resolver is executed after the policy " +
                    "has determined that the current user is allowed to access " +
                    "the field.")
                .Type<NonNullType<ApplyPolicyType>>()
                .DefaultValue(ApplyPolicy.BeforeResolver);

            descriptor.Use<AuthorizeMiddleware>();
        }
    }
}
