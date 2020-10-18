using System;
using HotChocolate.Data.Filters;

namespace HotChocolate
{
    /// <summary>
    /// Provides filtering extensions for the <see cref="ISchemaBuilder"/>.
    /// </summary>
    public static class FilterSchemaBuilderExtensions
    {
        /// <summary>
        /// Adds filtering support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddFiltering(
            this ISchemaBuilder builder) =>
            AddFiltering(builder, x => x.AddDefaults());

        /// <summary>
        /// Adds filtering support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <param name="configure">
        /// Configures the convention.
        /// </param>
        /// <param name="name">
        /// The filter convention name.
        /// </param>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddFiltering(
            this ISchemaBuilder builder,
            Action<IFilterConventionDescriptor> configure,
            string? name = null) =>
            builder
                .TryAddConvention<IFilterConvention>(sp => new FilterConvention(configure), name)
                .TryAddTypeInterceptor<FilterTypeInterceptor>();

        /// <summary>
        /// Adds filtering support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="ISchemaBuilder"/>.
        /// </param>
        /// <param name="name">
        /// The filter convention name.
        /// </param>
        /// <typeparam name="TConvention">
        /// The concrete filter convention type.
        /// </typeparam>
        /// <returns>
        /// Returns the <see cref="ISchemaBuilder"/>.
        /// </returns>
        public static ISchemaBuilder AddFiltering<TConvention>(
            this ISchemaBuilder builder,
            string? name = null)
            where TConvention : class, IFilterConvention =>
            builder
                .TryAddConvention<IFilterConvention, TConvention>(name)
                .TryAddTypeInterceptor<FilterTypeInterceptor>();
    }
}
