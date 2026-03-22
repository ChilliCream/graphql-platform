using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder AddValidationVisitor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder,
        bool isCacheable = true)
        where T : DocumentValidatorVisitor, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        return ConfigureValidation(builder, (_, b) => b.AddVisitor<T>(isCacheable: isCacheable));
    }

    public static IFusionGatewayBuilder AddValidationVisitor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory,
        bool isCacheable = true)
        where T : DocumentValidatorVisitor
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return ConfigureValidation(
            builder,
            (_, b) => b.AddVisitor(factory, isCacheable: isCacheable));
    }

    public static IFusionGatewayBuilder AddValidationRule<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IDocumentValidatorRule, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        return ConfigureValidation(builder, (_, b) => b.AddRule<T>());
    }

    public static IFusionGatewayBuilder AddValidationRule<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory)
        where T : class, IDocumentValidatorRule
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return ConfigureValidation(builder, (_, b) => b.AddRule(factory));
    }

    public static IFusionGatewayBuilder AddMaxExecutionDepthRule(
        this IFusionGatewayBuilder builder,
        int maxAllowedExecutionDepth,
        bool skipIntrospectionFields = false,
        bool allowRequestOverrides = false,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.AddMaxExecutionDepthRule(
                maxAllowedExecutionDepth,
                skipIntrospectionFields,
                allowRequestOverrides,
                isEnabled));
        return builder;
    }

    public static IFusionGatewayBuilder DisableIntrospection(
        this IFusionGatewayBuilder builder,
        bool disable = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return ConfigureValidation(
            builder,
            (_, b) => b.ModifyOptions(o => o.DisableIntrospection = disable));
    }

    public static IFusionGatewayBuilder DisableIntrospection(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, ValidationOptions, bool> disable)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(disable);

        return ConfigureValidation(
            builder,
            (s, b) => b.ModifyOptions(o => o.DisableIntrospection = disable(s, o)));
    }

    public static IFusionGatewayBuilder SetMaxAllowedValidationErrors(
        this IFusionGatewayBuilder builder,
        int maxAllowedValidationErrors)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.ModifyOptions(o => o.MaxAllowedErrors = maxAllowedValidationErrors));

        return builder;
    }

    public static IFusionGatewayBuilder SetMaxAllowedLocationsPerValidationError(
        this IFusionGatewayBuilder builder,
        int maxLocationsPerError)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.ModifyOptions(o => o.MaxLocationsPerError = maxLocationsPerError));

        return builder;
    }

    public static IFusionGatewayBuilder SetIntrospectionAllowedDepth(
        this IFusionGatewayBuilder builder,
        ushort maxAllowedOfTypeDepth,
        ushort maxAllowedListRecursiveDepth)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.ModifyOptions(o =>
            {
                o.MaxAllowedOfTypeDepth = maxAllowedOfTypeDepth;
                o.MaxAllowedListRecursiveDepth = maxAllowedListRecursiveDepth;
            }));

        return builder;
    }

    public static IFusionGatewayBuilder AddMaxAllowedFieldCycleDepthRule(
        this IFusionGatewayBuilder builder,
        ushort? defaultCycleLimit = 3,
        (SchemaCoordinate Coordinate, ushort MaxAllowed)[]? coordinateCycleLimits = null,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(
            builder,
            (_, b) => b.AddMaxAllowedFieldCycleDepthRule(
                defaultCycleLimit,
                coordinateCycleLimits,
                isEnabled));

        return builder;
    }

    public static IFusionGatewayBuilder RemoveMaxAllowedFieldCycleDepthRule(
        this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureValidation(builder, (_, b) => b.RemoveMaxAllowedFieldCycleDepthRule());
        return builder;
    }

    public static IFusionGatewayBuilder ConfigureValidation(
        this IFusionGatewayBuilder builder,
        Action<IServiceProvider, DocumentValidatorBuilder> configure)
    {
        return FusionSetupUtilities.Configure(
            builder,
            options => options.DocumentValidatorBuilderModifiers.Add(configure));
    }
}
