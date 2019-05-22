#if ASPNETCLASSIC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Microsoft.Owin;

namespace AspNetCore
{
    public interface IOwinContextAccessor
    {
        IOwinContext OwinContext { get; }
    }

    public class OwinContextAccessor
        : IOwinContextAccessor
    {
        private static AsyncLocal<OwinContextHolder> _httpContextCurrent =
            new AsyncLocal<OwinContextHolder>();

        public HttpContext HttpContext
        {
            get
            {
                return  _httpContextCurrent.Value?.Context;
            }
            set
            {
                var holder = _httpContextCurrent.Value;

                if (holder != null)
                {
                    holder.Context = null;
                }

                if (value != null)
                {
                    _httpContextCurrent.Value = new HttpContextHolder
                    {
                        Context = value
                    };
                }
            }
        }

        private class OwinContextHolder
        {
            public IOwinContext Context;
        }
    }
}
#endif
