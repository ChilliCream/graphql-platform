using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization.Properties;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeValidationResultAggregator : IValidationResultAggregator
{
    private readonly IAuthorizationHandler _handler;
    private readonly IServiceProvider _services;

    public AuthorizeValidationResultAggregator(
        IAuthorizationHandler handler,
        IServiceProvider services)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public async ValueTask AggregateAsync(
        IDocumentValidatorContext context,
        DocumentNode document,
        CancellationToken ct)
    {
        if (context.ContextData.TryGetValue(AuthContextData.Directives, out var value) &&
            value is AuthorizeDirective[] directives)
        {
            var ctx = new AuthorizationContext(
                context.Schema,
                _services,
                context.ContextData,
                document,
                context.DocumentId);

            var result = await _handler.AuthorizeAsync(ctx, directives, ct).ConfigureAwait(false);

            if (result is not AuthorizeResult.Allowed)
            {
                context.ContextData[WellKnownContextData.HttpStatusCode] = 401;
                context.ReportError(CreateError(result));
            }
        }
    }

    private IError CreateError(AuthorizeResult result)
    {
        return result switch
        {
            AuthorizeResult.NoDefaultPolicy
                => ErrorBuilder.New()
                    .SetMessage(AuthCoreResources.AuthorizeMiddleware_NoDefaultPolicy)
                    .SetCode(ErrorCodes.Authentication.NoDefaultPolicy)
                    .Build(),

            AuthorizeResult.PolicyNotFound
                => ErrorBuilder.New()
                    .SetMessage(AuthCoreResources.AuthorizeMiddleware_PoliciesMissing)
                    .SetCode(ErrorCodes.Authentication.PolicyNotFound)
                    .Build(),
            _
                => ErrorBuilder.New()
                    .SetMessage(AuthCoreResources.AuthorizeMiddleware_NotAuthorized)
                    .SetCode(result == AuthorizeResult.NotAllowed
                        ? ErrorCodes.Authentication.NotAuthorized
                        : ErrorCodes.Authentication.NotAuthenticated)
                    .Build()
        };
    }
}
