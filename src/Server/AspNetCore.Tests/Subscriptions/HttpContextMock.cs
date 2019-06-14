using System;
using System.Security.Claims;
using System.Threading;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class HttpContextMock
        : IHttpContext
    {
        public object User { get; set; }

        public CancellationToken RequestAborted { get; }

        public IServiceProvider RequestServices { get; }

        public void AddIdentity(ClaimsIdentity identity)
        {
        }
    }
}
