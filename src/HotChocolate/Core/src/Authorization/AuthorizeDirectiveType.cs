using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeDirectiveType : DirectiveType<AuthorizeDirective>, ISchemaDirective
{
    public AuthorizeDirectiveType()
    {
        Name = Names.Authorize;
    }

    protected override void Configure(IDirectiveTypeDescriptor<AuthorizeDirective> descriptor)
    {
        descriptor
            .Name(Names.Authorize)
            .Description("The authorize directive.")
            .Location(DirectiveLocation.Object)
            .Location(DirectiveLocation.FieldDefinition)
            .Repeatable()
            .Internal();

        descriptor
            .Argument(t => t.Policy)
            .Name(Names.Policy)
            .Description(
                "The name of the authorization policy that determines " +
                "access to the annotated resource.")
            .Type<StringType>();

        descriptor
            .Argument(t => t.Roles)
            .Name(Names.Roles)
            .Description(
                "Roles that are allowed to access the " +
                "annotated resource.")
            .Type<ListType<NonNullType<StringType>>>();

        descriptor
            .Argument(t => t.Apply)
            .Name(Names.Apply)
            .Description(
                "Defines when when the authorize directive shall be applied." +
                "By default the authorize directives are applied during the validation phase.")
            .Type<NonNullType<ApplyPolicyType>>()
            .DefaultValue(ApplyPolicy.BeforeResolver);

        descriptor.Use(CreateMiddleware());
    }

    public void ApplyConfiguration(
        IDescriptorContext context,
        DirectiveNode directiveNode,
        IDefinition definition,
        Stack<IDefinition> path)
    {
        ((IHasDirectiveDefinition)definition).Directives.Add(new(directiveNode));

        if (IsValidationAuthRule(directiveNode))
        {
            context.ContextData[WellKnownContextData.AuthorizationRequestPolicy] = true;
        }

        static bool IsValidationAuthRule(DirectiveNode directiveNode)
        {
            var args = directiveNode.Arguments;

            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                if (arg.Name.Value.EqualsOrdinal(Names.Apply) &&
                    arg.Value is EnumValueNode value &&
                    value.Value.EqualsOrdinal(ApplyPolicyType.Names.Validation))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static DirectiveMiddleware CreateMiddleware()
        => (next, directive) =>
        {
            var value = directive.AsValue<AuthorizeDirective>();
            var auth = new AuthorizeMiddleware(next, value);
            return async context => await auth.InvokeAsync(context).ConfigureAwait(false);
        };

    public static class Names
    {
        public const string Authorize = "authorize";
        public const string Policy = "policy";
        public const string Roles = "roles";
        public const string Apply = "apply";
    }
}
