using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using HotChocolate.RateLimit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore.RateLimit
{
    internal class LimitContext : ILimitContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LimitContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public RequestIdentity CreateRequestIdentity(
            IReadOnlyCollection<IPolicyIdentifier> identifiers, Path path)
        {
            return RequestIdentity.Create(path.ToString(), GetIds(identifiers).ToArray());
        }

        private IEnumerable<string> GetIds(IReadOnlyCollection<IPolicyIdentifier> identifiers)
        {
            foreach (IPolicyIdentifier identifier in identifiers)
            {
                switch (identifier)
                {
                    case ClaimsPolicyIdentifier userPolicyScope:
                        yield return GetUserIdentity(userPolicyScope);
                        break;

                    case HeaderPolicyIdentifier clientIdPolicyScope:
                        yield return GetClientId(clientIdPolicyScope.Header);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unknown policy identifier type of {identifier.GetType().Name}");
                }
            }
        }

        private string GetUserIdentity(ClaimsPolicyIdentifier identifier)
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            Claim? claim = httpContext?.User?.Claims
                .FirstOrDefault(c => c.Type == identifier.ClaimType);

            if (claim != null)
            {
                return claim.Value;
            }

            return string.Empty;
        }

        private string GetClientId(string header)
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;
            if (httpContext.Request.Headers.TryGetValue(header, out StringValues values))
            {
                return values.ToString();
            }

            return string.Empty;
        }
    }
}
