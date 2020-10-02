using System;
using System.Threading.Tasks;
using HotChocolate.RateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.RateLimit
{
    internal class HeaderPolicyIdentifier : IPolicyIdentifier
    {
        public HeaderPolicyIdentifier(string header)
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentException(
                    "Value cannot be null or empty.", nameof(header));
            }

            Header = header;
        }

        public string Header { get; }

        public ValueTask<string> ResolveAsync(IServiceProvider serviceProvider)
        {
            HttpContext? httpContext = serviceProvider
                .GetService<IHttpContextAccessor>()?.HttpContext;

            if (httpContext != null
                && httpContext.Request.Headers.TryGetValue(Header, out StringValues values))
            {
                return new ValueTask<string>(values.ToString());
            }

            return new ValueTask<string>(string.Empty);
        }
    }
}
