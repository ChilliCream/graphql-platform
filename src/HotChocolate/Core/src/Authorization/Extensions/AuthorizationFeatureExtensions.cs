using System.Collections.Immutable;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Types;

namespace HotChocolate.Authorization;

internal static class AuthorizationFeatureExtensions
{
    public static bool AreAuthorizeDirectivesRegistered(
        this ISchemaBuilder featureProvider)
        => featureProvider.Features.Get<AuthorizationFeature>()?.AreDirectivesRegistered ?? false;

    public static ISchemaBuilder MarkAuthorizeDirectivesRegistered(
        this ISchemaBuilder builder)
    {
        var feature = builder.Features.Get<AuthorizationFeature>() ?? new AuthorizationFeature(false);
        builder.Features.Set(feature with { AreDirectivesRegistered = true });
        return builder;
    }

    public static IObjectFieldDescriptor MarkForRequestLevelAuthorization(
        this IObjectFieldDescriptor descriptor)
    {
        var features = descriptor.Extend().Configuration.Features;
        var options = features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        features.Set(options with { AuthorizeAtRequestLevel = true });
        return descriptor;
    }

    public static IObjectFieldDescriptor AllowAnonymous(
        this IObjectFieldDescriptor descriptor)
    {
        var features = descriptor.Extend().Configuration.Features;
        var options = features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        features.Set(options with { AllowAnonymous = true });
        return descriptor;
    }

    public static IRequestContext EnsureAuthorizationRequestInfoExists(
        this IRequestContext context)
    {
        context.Features.GetOrSet<AuthorizationRequestInfo>();
        return context;
    }

    public static IAuthorizationHandler GetAuthorizationHandler(
        this IRequestContext context)
    {
        var info = context.Features.Get<AuthorizationRequestInfo>();
        return info?.Handler ?? throw new InvalidOperationException("Authorization handler not found.");
    }

    public static ImmutableArray<AuthorizeDirective> GetAuthorizeDirectives(
        this IRequestContext context)
    {
        var info = context.Features.Get<AuthorizationRequestInfo>();
        return info?.Directives ?? [];
    }
}
