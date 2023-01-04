using System;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Authorization;

public sealed class AuthorizeDirectiveType : DirectiveType<AuthorizeDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<AuthorizeDirective> descriptor)
    {
        descriptor
            .Name(Names.Authorize)
            .Location(DirectiveLocation.Object)
            .Location(DirectiveLocation.FieldDefinition)
            .Repeatable()
            .Internal();

        descriptor
            .Argument(t => t.Policy)
            .Description(
                "The name of the authorization policy that determines " +
                "access to the annotated resource.")
            .Type<StringType>();

        descriptor
            .Argument(t => t.Roles)
            .Description(
                "Roles that are allowed to access the " +
                "annotated resource.")
            .Type<ListType<NonNullType<StringType>>>();

        descriptor
            .Argument(t => t.Apply)
            .Description(
                "Defines when when the resolver shall be executed." +
                "By default the resolver is executed after the policy " +
                "has determined that the current user is allowed to access " +
                "the field.")
            .Type<NonNullType<ApplyPolicyType>>()
            .DefaultValue(ApplyPolicy.BeforeResolver);

        var context = descriptor.Extend().Context;
        descriptor.Use(CreateMiddleware(context.Services));
    }

    private static DirectiveMiddleware CreateMiddleware(
        IServiceProvider schemaServices)
    {
        return (next, directive) =>
        {
            var value = directive.AsValue<AuthorizeDirective>();
            var handler = schemaServices.GetApplicationService<IAuthorizationHandler>();
            var auth = new AuthorizeMiddleware(next, handler, value);
            return async context => await auth.InvokeAsync(context).ConfigureAwait(false);
        };
    }

    public static class Names
    {
        public const string Authorize = "authorize";
    }
}
