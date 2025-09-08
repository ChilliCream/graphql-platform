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
        var feature = builder.Features.Get<AuthorizationFeature>();

        if (feature is null)
        {
            builder.Features.Set(new AuthorizationFeature(AreDirectivesRegistered: true));
        }

        return builder;
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

    public static SchemaTypeConfiguration ModifyAuthorizationFieldOptions(
        this SchemaTypeConfiguration context,
        Func<AuthorizationFieldOptions, AuthorizationFieldOptions> configure)
    {
        var features = context.Features;
        var options = features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        features.Set(configure(options));
        return context;
    }

    public static IDescriptorContext ModifyAuthorizationFieldOptions(
        this IDescriptorContext context,
        Func<AuthorizationFieldOptions, AuthorizationFieldOptions> configure)
    {
        var features = context.Features;
        var options = features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        features.Set(configure(options));
        return context;
    }

    public static IObjectFieldDescriptor ModifyAuthorizationFieldOptions(
        this IObjectFieldDescriptor descriptor,
        Func<AuthorizationFieldOptions, AuthorizationFieldOptions> configure)
    {
        ModifyAuthorizationFieldOptions(descriptor.Extend().Configuration, configure);
        return descriptor;
    }

    public static ObjectFieldConfiguration ModifyAuthorizationFieldOptions(
        this ObjectFieldConfiguration configuration,
        Func<AuthorizationFieldOptions, AuthorizationFieldOptions> configure)
    {
        var options = configuration.Features.Get<AuthorizationFieldOptions>() ?? new AuthorizationFieldOptions();
        configuration.Features.Set(configure(options));
        return configuration;
    }

    public static bool IsAnonymousAllowed(
        this ObjectFieldConfiguration configuration)
    {
        var options = configuration.Features.Get<AuthorizationFieldOptions>();
        return options?.AllowAnonymous ?? false;
    }

    public static RequestContext EnsureAuthorizationRequestDataExists(
        this RequestContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestContext>();
        data.Handler = context.RequestServices.GetRequiredService<IAuthorizationHandler>();
        return context;
    }

    public static IAuthorizationHandler GetAuthorizationHandler(
        this RequestContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestContext>();
        return data.Handler ?? throw new InvalidOperationException("Authorization handler not found.");
    }

    public static IAuthorizationHandler GetAuthorizationHandler(
        this IMiddlewareContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestContext>();
        return data.Handler ?? throw new InvalidOperationException("Authorization handler not found.");
    }

    public static ImmutableArray<AuthorizeDirective> GetAuthorizeDirectives(
        this RequestContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestContext>();
        return data.Directives;
    }

    public static ImmutableArray<AuthorizeDirective> GetAuthorizeDirectives(
        this DocumentValidatorContext context)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestContext>();
        return data.Directives;
    }

    public static DocumentValidatorContext SetAuthorizeDirectives(
        this DocumentValidatorContext context,
        ImmutableArray<AuthorizeDirective> directives)
    {
        var data = context.Features.GetOrSet<AuthorizationRequestContext>();
        data.Directives = directives;
        return context;
    }

    public static AuthorizationOptions GetAuthorizationOptions(
        this IDescriptorContext context)
        => context.Features.GetOrSet<AuthorizationOptions>();

    public static AuthorizationOptions GetAuthorizationOptions(
        this ISchemaBuilder builder)
        => builder.Features.GetOrSet<AuthorizationOptions>();

    public static RequestContext TryCreateUserStateIfNotExists(
        this RequestContext context)
    {
        var userState = context.Features.Get<UserState>();

        if (userState is null
            && context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var value)
            && value is ClaimsPrincipal principal)
        {
            context.Features.Set(new UserState(principal));
        }

        return context;
    }
}
