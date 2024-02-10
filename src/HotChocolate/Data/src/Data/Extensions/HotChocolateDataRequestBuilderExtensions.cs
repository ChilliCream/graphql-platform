using System;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class HotChocolateDataRequestBuilderExtensions
{
    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The filter convention name.
    /// </param>
    /// <param name="compatabilityMode">
    /// If true uses the old naming convention
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddFiltering(
        this IRequestExecutorBuilder builder,
        string? name = null,
        bool compatabilityMode = false)
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new FilterContextParameterExpressionBuilder());

        return builder.ConfigureSchema(s => s.AddFiltering(name, compatabilityMode));
    }

    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// Configures the convention.
    /// </param>
    /// <param name="name">
    /// The filter convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddFiltering(
        this IRequestExecutorBuilder builder,
        Action<IFilterConventionDescriptor> configure,
        string? name = null)
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new FilterContextParameterExpressionBuilder());

        return builder
            .ConfigureSchema(s => s.AddFiltering(configure, name));
    }

    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The filter convention name.
    /// </param>
    /// <typeparam name="TConvention">
    /// The concrete filter convention type.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddFiltering<TConvention>(
        this IRequestExecutorBuilder builder,
        string? name = null)
        where TConvention : class, IFilterConvention
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new FilterContextParameterExpressionBuilder());

        return builder.ConfigureSchema(s => s.AddFiltering<TConvention>(name));
    }

    /// <summary>
    /// Adds sorting support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The sort convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddSorting(
        this IRequestExecutorBuilder builder,
        string? name = null)
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new SortingContextParameterExpressionBuilder());
        return builder.ConfigureSchema(s => s.AddSorting(name));
    }

    /// <summary>
    /// Adds sorting support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// Configures the convention.
    /// </param>
    /// <param name="name">
    /// The sort convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddSorting(
        this IRequestExecutorBuilder builder,
        Action<ISortConventionDescriptor> configure,
        string? name = null)
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new SortingContextParameterExpressionBuilder());
        return builder.ConfigureSchema(s => s.AddSorting(configure, name));
    }

    /// <summary>
    /// Adds sorting support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The sort convention name.
    /// </param>
    /// <typeparam name="TConvention">
    /// The concrete sort convention type.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddSorting<TConvention>(
        this IRequestExecutorBuilder builder,
        string? name = null)
        where TConvention : class, ISortConvention
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new SortingContextParameterExpressionBuilder());
        return builder.ConfigureSchema(s => s.AddSorting<TConvention>(name));
    }

    /// <summary>
    /// Adds projections support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The projection convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddProjections(
        this IRequestExecutorBuilder builder,
        string? name = null) =>
        AddProjections(builder, x => x.AddDefaults(), name);

    /// <summary>
    /// Adds projection support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// Configures the convention.
    /// </param>
    /// <param name="name">
    /// The projection convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddProjections(
        this IRequestExecutorBuilder builder,
        Action<IProjectionConventionDescriptor> configure,
        string? name = null)
        => builder.ConfigureSchema(s => s
            .TryAddTypeInterceptor<ProjectionTypeInterceptor>()
            .TryAddConvention<IProjectionConvention>(
                _ => new ProjectionConvention(configure),
                name));

    /// <summary>
    /// Adds projection support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The projection convention name.
    /// </param>
    /// <typeparam name="TConvention">
    /// The concrete projection convention type.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddProjections<TConvention>(
        this IRequestExecutorBuilder builder,
        string? name = null)
        where TConvention : class, IProjectionConvention
        => builder.ConfigureSchema(s => s
            .TryAddTypeInterceptor<ProjectionTypeInterceptor>()
            .TryAddConvention<IProjectionConvention, TConvention>(name));
}
