using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
#if ASPNETCLASSIC
using HotChocolate.AspNetClassic;
using HttpContext = Microsoft.Owin.IOwinContext;
#else
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;
#endif
namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class HttpContextWrapper
        : IHttpContext
    {
        private readonly HttpContext _context;
        private IPrincipal _user;

        public HttpContextWrapper(
            HttpContext context)
        {
            _context = context;
        }

        public IPrincipal User
        {
            get
            {
                if (_user == null)
                {
                    return _context.GetUser();
                }

                return _user;
            } 
            set => _user = value;
        }

        public CancellationToken RequestAborted =>
            _context.GetCancellationToken();

        public void AddIdentity(ClaimsIdentity identity)
        {
#if ASPNETCLASSIC
            _context.Authentication.User.AddIdentity(identity);
#else
            _context.User.AddIdentity(identity);
#endif
        }

#if !ASPNETCLASSIC
        public IServiceProvider RequestServices =>
            _context.RequestServices;
#endif
    }
}
