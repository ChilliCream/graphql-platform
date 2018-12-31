using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;

#if !ASPNETCLASSIC
using Microsoft.AspNetCore.Authorization;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Authorization
#else
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

            descriptor.Middleware(
                next => context => AuthorizeAsync(context, next));
        }

        private static async Task AuthorizeAsync(
            IDirectiveContext context,
            DirectiveDelegate next)
        {
#if !ASPNETCLASSIC
            IAuthorizationService authorizeService = context
                .Service<IAuthorizationService>();
            IAuthorizationPolicyProvider policyProvider =
                context.Service<IAuthorizationPolicyProvider>();
#endif
            ClaimsPrincipal principal = context
                .CustomProperty<ClaimsPrincipal>(nameof(ClaimsPrincipal));
            AuthorizeDirective directive = context.Directive
                .ToObject<AuthorizeDirective>();
            

            if(!IsInRoles(principal, directive.Roles))
            {
                context.Result = BuildUnauthorizedError(context);
                return;
            }

#if !ASPNETCLASSIC
            
            AuthorizationPolicy policy = null;
            if (string.IsNullOrWhiteSpace(directive.Policy))
                policy = await policyProvider.GetDefaultPolicyAsync();
            else
                policy = await policyProvider.GetPolicyAsync(directive.Policy);

            if(policy == null)
            {
                context.Result = BuildUnauthorizedError(context, true);
                return;
            }

            
            AuthorizationResult result =
                await authorizeService.AuthorizeAsync(principal, policy);

            if(!result.Succeeded)
            {
                context.Result = BuildUnauthorizedError(context);
                return;
            }
            
#endif
            await next(context);
            
        }

        private static QueryError BuildUnauthorizedError(IDirectiveContext context, bool policyNotfound = false)
        {
            string message =
                "The current user is not authorized to access this resource.";
            if (policyNotfound)
                message += " A valid authorization policy could not be found.";

            return QueryError.CreateFieldError(message,
                    context.Path,
                    context.FieldSelection);
        }

        private static bool IsInRoles(
            IPrincipal principal,
            IReadOnlyCollection<string> roles)
        {
            if (roles != null)
            {
                foreach (string role in roles)
                {
                    if (!principal.IsInRole(role))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
