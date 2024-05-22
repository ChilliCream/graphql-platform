using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization.Properties;
using HotChocolate.Language;
using HotChocolate.Validation;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeValidationResultAggregator : IValidationResultAggregator
{
    private readonly IServiceProvider _services;

    public AuthorizeValidationResultAggregator(
        IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public async ValueTask AggregateAsync(
        IDocumentValidatorContext context,
        DocumentNode document,
        CancellationToken ct)
    {
        if (!context.ContextData.TryGetValue(AuthorizationHandler, out var handlerValue) ||
            handlerValue is not IAuthorizationHandler handler)
        {
            throw new MissingStateException(
                "Authorization",
                AuthorizationHandler,
                StateKind.Global);
        }

        if (context.ContextData.TryGetValue(AuthContextData.Directives, out var value) &&
            value is AuthorizeDirective[] directives)
        {
            var ctx = new AuthorizationContext(
                context.Schema,
                _services,
                context.ContextData,
                document,
                context.DocumentId.Value);

            var result = await handler.AuthorizeAsync(ctx, directives, ct).ConfigureAwait(false);

            if (result is not AuthorizeResult.Allowed)
            {
                context.ContextData[HttpStatusCode] = 401;
                context.ReportError(CreateError(result));
            }
        }
    }

    private static IError CreateError(AuthorizeResult result)
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
                    .Build(),
        };
    }
}
