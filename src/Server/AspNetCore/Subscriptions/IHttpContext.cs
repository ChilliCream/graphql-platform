using System;
using System.Security.Claims;
using System.Threading;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public interface IHttpContext
    {
        ClaimsPrincipal User { get; set; }

        CancellationToken RequestAborted { get; }

#if !ASPNETCLASSIC
        IServiceProvider RequestServices { get; }
#endif

        void AddIdentity(ClaimsIdentity identity);
    }
}
