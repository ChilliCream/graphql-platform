using System.Threading;
using Microsoft.Owin;

namespace HotChocolate.AspNetClassic
{
    public class OwinContextAccessor
        : IOwinContextAccessor
    {
        private static AsyncLocal<OwinContextHolder> _httpContextCurrent =
            new AsyncLocal<OwinContextHolder>();

        public IOwinContext OwinContext
        {
            get
            {
                return  _httpContextCurrent.Value?.Context;
            }
            set
            {
                OwinContextHolder holder = _httpContextCurrent.Value;

                if (holder != null)
                {
                    holder.Context = null;
                }

                if (value != null)
                {
                    _httpContextCurrent.Value = new OwinContextHolder
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
