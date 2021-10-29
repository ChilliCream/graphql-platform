using System.Security.Claims;
using System.Threading.Tasks;

namespace HotChocolate.Authorization
{
    public interface IAuthorizationHandler
    {
        Task<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            ClaimsPrincipal principal,
            AuthorizeDirective directive);
    }

    public enum AuthorizeResult
    {
        Allowed,
        NotAllowed,
        NotAuthenticated,
        NoDefaultPolicy,
        PolicyNotFound
    }
}
