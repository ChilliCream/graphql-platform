using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Validation;
using HotChocolate.Validation.Options;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds a document validation visitor to the router.
    /// </summary>
    public static IFusionRouterBuilder AddValidationVisitor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder,
        bool isCacheable = true)
        where T : DocumentValidatorVisitor, new()
    {
        CoreFusionGatewayBuilderExtensions.AddValidationVisitor<T>(builder, isCacheable);
        return builder;
    }

    /// <summary>
    /// Adds a document validation visitor created by the specified factory.
    /// </summary>
    public static IFusionRouterBuilder AddValidationVisitor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory,
        bool isCacheable = true)
        where T : DocumentValidatorVisitor
    {
        CoreFusionGatewayBuilderExtensions.AddValidationVisitor(builder, factory, isCacheable);
        return builder;
    }

    /// <summary>
    /// Adds a document validation rule to the router.
    /// </summary>
    public static IFusionRouterBuilder AddValidationRule<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder)
        where T : class, IDocumentValidatorRule, new()
    {
        CoreFusionGatewayBuilderExtensions.AddValidationRule<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a document validation rule created by the specified factory.
    /// </summary>
    public static IFusionRouterBuilder AddValidationRule<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, ValidationOptions, T> factory)
        where T : class, IDocumentValidatorRule
    {
        CoreFusionGatewayBuilderExtensions.AddValidationRule(builder, factory);
        return builder;
    }

    /// <summary>
    /// Limits the execution depth of operations.
    /// </summary>
    public static IFusionRouterBuilder AddMaxExecutionDepthRule(
        this IFusionRouterBuilder builder,
        int maxAllowedExecutionDepth,
        bool skipIntrospectionFields = false,
        bool allowRequestOverrides = false,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
    {
        CoreFusionGatewayBuilderExtensions.AddMaxExecutionDepthRule(
            builder,
            maxAllowedExecutionDepth,
            skipIntrospectionFields,
            allowRequestOverrides,
            isEnabled);
        return builder;
    }

    /// <summary>
    /// Configures whether introspection is disabled.
    /// </summary>
    public static IFusionRouterBuilder DisableIntrospection(
        this IFusionRouterBuilder builder,
        bool disable = true)
    {
        CoreFusionGatewayBuilderExtensions.DisableIntrospection(builder, disable);
        return builder;
    }

    /// <summary>
    /// Configures whether introspection is disabled using the specified predicate.
    /// </summary>
    public static IFusionRouterBuilder DisableIntrospection(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, ValidationOptions, bool> disable)
    {
        CoreFusionGatewayBuilderExtensions.DisableIntrospection(builder, disable);
        return builder;
    }

    /// <summary>
    /// Limits the number of validation errors.
    /// </summary>
    public static IFusionRouterBuilder SetMaxAllowedValidationErrors(
        this IFusionRouterBuilder builder,
        int maxAllowedValidationErrors)
    {
        CoreFusionGatewayBuilderExtensions.SetMaxAllowedValidationErrors(builder, maxAllowedValidationErrors);
        return builder;
    }

    /// <summary>
    /// Limits the number of locations reported for each validation error.
    /// </summary>
    public static IFusionRouterBuilder SetMaxAllowedLocationsPerValidationError(
        this IFusionRouterBuilder builder,
        int maxLocationsPerError)
    {
        CoreFusionGatewayBuilderExtensions.SetMaxAllowedLocationsPerValidationError(builder, maxLocationsPerError);
        return builder;
    }

    /// <summary>
    /// Limits recursive introspection depth.
    /// </summary>
    public static IFusionRouterBuilder SetIntrospectionAllowedDepth(
        this IFusionRouterBuilder builder,
        ushort maxAllowedOfTypeDepth,
        ushort maxAllowedListRecursiveDepth)
    {
        CoreFusionGatewayBuilderExtensions.SetIntrospectionAllowedDepth(
            builder,
            maxAllowedOfTypeDepth,
            maxAllowedListRecursiveDepth);
        return builder;
    }

    /// <summary>
    /// Limits field merge comparisons during validation.
    /// </summary>
    public static IFusionRouterBuilder SetMaxAllowedFieldMergeComparisons(
        this IFusionRouterBuilder builder,
        int maxAllowedFieldMergeComparisons)
    {
        CoreFusionGatewayBuilderExtensions.SetMaxAllowedFieldMergeComparisons(builder, maxAllowedFieldMergeComparisons);
        return builder;
    }

    /// <summary>
    /// Adds field cycle depth limits, optionally overriding individual schema coordinates.
    /// </summary>
    public static IFusionRouterBuilder AddMaxAllowedFieldCycleDepthRule(
        this IFusionRouterBuilder builder,
        ushort? defaultCycleLimit = 3,
        (SchemaCoordinate Coordinate, ushort MaxAllowed)[]? coordinateCycleLimits = null,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
    {
        CoreFusionGatewayBuilderExtensions.AddMaxAllowedFieldCycleDepthRule(
            builder,
            defaultCycleLimit,
            coordinateCycleLimits,
            isEnabled);
        return builder;
    }

    /// <summary>
    /// Removes the field cycle depth validation rule.
    /// </summary>
    public static IFusionRouterBuilder RemoveMaxAllowedFieldCycleDepthRule(this IFusionRouterBuilder builder)
    {
        CoreFusionGatewayBuilderExtensions.RemoveMaxAllowedFieldCycleDepthRule(builder);
        return builder;
    }

    /// <summary>
    /// Registers a callback to configure document validation for the router schema.
    /// </summary>
    public static IFusionRouterBuilder ConfigureValidation(
        this IFusionRouterBuilder builder,
        Action<IServiceProvider, DocumentValidatorBuilder> configure)
    {
        CoreFusionGatewayBuilderExtensions.ConfigureValidation(builder, configure);
        return builder;
    }
}
