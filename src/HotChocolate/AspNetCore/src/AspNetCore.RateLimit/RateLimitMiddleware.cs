using System;
using System.Threading.Tasks;
using HotChocolate.RateLimit;
using HotChocolate.Resolvers;
using Microsoft.Extensions.Options;

namespace HotChocolate.AspNetCore.RateLimit
{
    internal class RateLimitMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly ILimitProcessor _limitProcessor;
        private readonly RateLimitOptions _options;

        public RateLimitMiddleware(
            FieldDelegate next, ILimitProcessor limitProcessor, IOptions<RateLimitOptions> options)
        {
            _next = next;
            _limitProcessor = limitProcessor;
            _options = options.Value;
        }

        public async Task InvokeAsync(
            IDirectiveContext directiveContext,
            IRateLimitContext rateLimitContext)
        {
            LimitDirective directive = directiveContext.Directive.ToObject<LimitDirective>();

            if (!_options.Policies.ContainsKey(directive.Policy))
            {
                throw new InvalidOperationException(
                    $"Could not find registered policy scope {directive.Policy}");
            }

            LimitPolicy policy = _options.Policies[directive.Policy];

            RequestIdentity requestIdentity = await rateLimitContext
                .CreateRequestIdentityAsync(policy.Identifiers, directiveContext.Path);

            if (requestIdentity.IsEmpty)
            {
                await _next(directiveContext);
            }
            else
            {
                Limit limit = await _limitProcessor.ExecuteAsync(
                    requestIdentity, policy, directiveContext.RequestAborted);

                if (limit.IsValid(policy))
                {
                    await _next(directiveContext);
                }
                else
                {
                    directiveContext.Result = ErrorBuilder.New()
                        .SetMessage("The maximum number of requests was exceeded.")
                        .SetCode("REQUEST_LIMIT_EXCEEDED")
                        .SetPath(directiveContext.Path)
                        .AddLocation(directiveContext.FieldSelection)
                        .Build();
                }
            }
        }
    }
}
