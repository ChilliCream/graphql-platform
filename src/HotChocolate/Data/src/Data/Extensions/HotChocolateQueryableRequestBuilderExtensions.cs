using System;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions.Configuration;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class HotChocolateQueryableRequestBuilderExtensions
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
    public static IRequestExecutorBuilder AddQueryableFiltering(
        this IRequestExecutorBuilder builder,
        string? name = null,
        bool compatabilityMode = false)
        => builder.AddFiltering(name, compatabilityMode);

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
    public static IRequestExecutorBuilder AddQueryableFiltering(
        this IRequestExecutorBuilder builder,
        Action<IQueryableFilterConventionDescriptor> configure,
        string? name = null)
    {
        void Wrap(IFilterConventionDescriptor d)
            => configure(new QueryableFilterConventionDescriptor(d));

        return builder.AddFiltering(Wrap, name);
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
    public static IRequestExecutorBuilder AddQueryableFiltering<TConvention>(
        this IRequestExecutorBuilder builder,
        string? name = null)
        where TConvention : class, IFilterConvention
        => builder.AddFiltering<TConvention>(name);

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
    public static IRequestExecutorBuilder AddQueryableSorting(
        this IRequestExecutorBuilder builder,
        string? name = null)
        => builder.AddSorting(name);

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
    public static IRequestExecutorBuilder AddQueryableSorting(
        this IRequestExecutorBuilder builder,
        Action<IQueryableSortConventionDescriptor> configure,
        string? name = null)
    {
        void Wrap(ISortConventionDescriptor d)
            => configure(new QueryableSortConventionDescriptor(d));

        return builder.AddSorting(Wrap, name);
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
    public static IRequestExecutorBuilder AddQueryableSorting<TConvention>(
        this IRequestExecutorBuilder builder,
        string? name = null)
        where TConvention : class, ISortConvention
        => builder.AddSorting<TConvention>(name);

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
    public static IRequestExecutorBuilder AddQueryableProjections(
        this IRequestExecutorBuilder builder,
        string? name = null)
        => builder.AddProjections(name);

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
    public static IRequestExecutorBuilder AddQueryableProjections(
        this IRequestExecutorBuilder builder,
        Action<IProjectionConventionDescriptor> configure,
        string? name = null)
    {
        void Wrap(IProjectionConventionDescriptor d)
            => configure(new QueryableProjectionConventionDescriptor(d));

        return builder.AddQueryableProjections(Wrap, name);
    }

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
    public static IRequestExecutorBuilder AddQueryableProjections<TConvention>(
        this IRequestExecutorBuilder builder,
        string? name = null)
        where TConvention : class, IProjectionConvention =>
        builder.AddProjections<TConvention>(name);
}
