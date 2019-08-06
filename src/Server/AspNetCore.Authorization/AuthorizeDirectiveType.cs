using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
#if ASPNETCLASSIC

namespace HotChocolate.AspNetClassic.Authorization
#else
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization
#endif
{
    public class AuthorizeDirectiveType
        : DirectiveType<AuthorizeDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<AuthorizeDirective> descriptor)
        {
            descriptor.Name("authorize");

            descriptor.Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.FieldDefinition);

#if !ASPNETCLASSIC
            // TODO :resources
            descriptor.Argument(t => t.Policy)
                .Description(
                    "The name of the authorization policy that determines " +
                    "access to the annotated resource.")
                .Type<StringType>();
#endif

            // TODO :resources
            descriptor.Argument(t => t.Roles)
                .Description(
                    "Roles that are allowed to access to the " +
                    "annotated resource.")
                .Type<ListType<NonNullType<StringType>>>();

            descriptor.Repeatable();

            descriptor.Use<AuthorizeMiddleware>();
        }
    }
}
