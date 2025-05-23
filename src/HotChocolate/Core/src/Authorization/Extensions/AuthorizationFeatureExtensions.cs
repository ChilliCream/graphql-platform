using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

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

     public static SchemaTypeConfiguration MarkForRequestLevelAuthorization(
        this SchemaTypeConfiguration configuration)
    {
        var features = configuration.Features;
        var options = features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        features.Set(options with { AuthorizeAtRequestLevel = true });
        return configuration;
    }

    public static IDescriptorContext MarkForRequestLevelAuthorization(
        this IDescriptorContext context)
    {
        var features = context.Features;
        var options = features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        features.Set(options with { AuthorizeAtRequestLevel = true });
        return context;
    }

    public static IObjectFieldDescriptor MarkForRequestLevelAuthorization(
        this IObjectFieldDescriptor descriptor)
    {
        var features = descriptor.Extend().Configuration.Features;
        var options = features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        features.Set(options with { AuthorizeAtRequestLevel = true });
        return descriptor;
    }

    public static bool IsAuthorizedAtRequestLevel(
        this SchemaTypeConfiguration configuration)
    {
        var options = configuration.Features.Get<AuthorizationFieldOptions>();
        return options?.AuthorizeAtRequestLevel ?? false;
    }

    public static bool IsAuthorizedAtRequestLevel(
        this ObjectFieldConfiguration configuration)
    {
        var options = configuration.Features.Get<AuthorizationFieldOptions>();
        return options?.AuthorizeAtRequestLevel ?? false;
    }

    public static bool IsAuthorizedAtRequestLevel(
        this IDescriptorContext context)
    {
        var options = context.Features.Get<AuthorizationFieldOptions>();
        return options?.AuthorizeAtRequestLevel ?? false;
    }

    public static bool IsAuthorizedAtRequestLevel(
        this ISchemaDefinition context)
    {
        var options = context.Features.Get<AuthorizationFieldOptions>();
        return options?.AuthorizeAtRequestLevel ?? false;
    }

    public static IObjectFieldDescriptor AllowAnonymous(
        this IObjectFieldDescriptor descriptor)
    {
        var features = descriptor.Extend().Configuration.Features;
        var options = features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        features.Set(options with { AllowAnonymous = true });
        return descriptor;
    }

    public static ObjectFieldConfiguration AllowAnonymous(
        this ObjectFieldConfiguration configuration)
    {;
        var options = configuration.Features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        configuration.Features.Set(options with { AllowAnonymous = true });
        return configuration;
    }

    public static bool IsAnonymousAllowed(
        this ObjectFieldConfiguration configuration)
    {
        var options = configuration.Features.Get<AuthorizationFieldOptions>();
        return options?.AllowAnonymous ?? false;
    }

    public static IRequestContext EnsureAuthorizationRequestDataExists(
        this IRequestContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestData>();
        data.Handler = context.Services.GetRequiredService<IAuthorizationHandler>();
        return context;
    }

    public static IAuthorizationHandler GetAuthorizationHandler(
        this IRequestContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestData>();
        return data.Handler ?? throw new InvalidOperationException("Authorization handler not found.");
    }

    public static IAuthorizationHandler GetAuthorizationHandler(
        this IMiddlewareContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestData>();
        return data.Handler ?? throw new InvalidOperationException("Authorization handler not found.");
    }

    public static ImmutableArray<AuthorizeDirective> GetAuthorizeDirectives(
        this IRequestContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestData>();
        return data.Directives;
    }

    public static ImmutableArray<AuthorizeDirective> GetAuthorizeDirectives(
        this DocumentValidatorContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestData>();
        return data.Directives;
    }

    public static DocumentValidatorContext SetAuthorizeDirectives(
        this DocumentValidatorContext context,
        ImmutableArray<AuthorizeDirective> directives)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestData>();
        data.Directives = directives;
        return context;
    }

    public static AuthorizationOptions GetAuthorizationOptions(
        this IDescriptorContext context)
        => context.Features.GetOrSet<AuthorizationOptions>();

    public static AuthorizationOptions GetAuthorizationOptions(
        this ISchemaBuilder builder)
        => builder.Features.GetOrSet<AuthorizationOptions>();

    public static IRequestContext TryCreateUserStateIfNotExists(
        this IRequestContext context)
    {
        var userState = context.Features.Get<UserState>();

        if(userState is null
            && context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var value)
            && value is ClaimsPrincipal principal)
        {
            context.Features.Set(new UserState(principal));
        }

        return context;
    }
}
