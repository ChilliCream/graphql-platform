#if ASPNETCLASSIC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Microsoft.Owin;
using HttpContext = Microsoft.Owin.IOwinContext;

namespace AspNetCore
{
    public interface IOwinContextAccessor
    {
        Microsoft.Owin.IOwinContext OwinContext { get; }
    }

    public class OwinContextAccessor
        : IOwinContextAccessor
    {
        private static Async

        Microsoft.Owin.IOwinContext OwinContext { get; }
    }
}
#endif
