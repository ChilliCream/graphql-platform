using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HotChocolate.RateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.RateLimit
{
    public class ClaimsPolicyIdentifier : IPolicyIdentifier
    {
        public ClaimsPolicyIdentifier(string claimType)
        {
            if (string.IsNullOrEmpty(claimType))
            {
                throw new ArgumentException(
                    "Value cannot be null or empty.", nameof(claimType));
            }

            ClaimType = claimType;
        }

        public string ClaimType { get; }

        public ValueTask<string> ResolveAsync(IServiceProvider serviceProvider)
        {
            HttpContext? httpContext = serviceProvider
                .GetService<IHttpContextAccessor>()?.HttpContext;

            Claim? claim = httpContext?.User.Claims
                .FirstOrDefault(c => c.Type == ClaimType);

            if (claim != null)
            {
                return new ValueTask<string>(claim.Value);
            }

            return new ValueTask<string>(string.Empty);
        }
    }
}
