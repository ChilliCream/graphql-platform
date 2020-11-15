using System;
using HotChocolate.Data.Projections;

namespace HotChocolate
{
    /// <summary>
    /// Provides filtering extensions for the <see cref="ISchemaBuilder"/>.
    /// </summary>
    public static class ProjectionsSchemaBuilderExtensions
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
        public static ISchemaBuilder AddProjections(
            this ISchemaBuilder builder) =>
            AddProjections(builder, x => x.AddDefaults());

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
        public static ISchemaBuilder AddProjections(
            this ISchemaBuilder builder,
            Action<IProjectionConventionDescriptor> configure,
            string? name = null) =>
            builder
                .TryAddTypeInterceptor<ProjectionTypeInterceptor>()
                .TryAddConvention<IProjectionConvention>(
                    sp => new ProjectionConvention(configure),
                    name);

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
        public static ISchemaBuilder AddProjections<TConvention>(
            this ISchemaBuilder builder,
            string? name = null)
            where TConvention : class, IProjectionConvention =>
            builder
                .TryAddTypeInterceptor<ProjectionTypeInterceptor>()
                .TryAddConvention<IProjectionConvention, TConvention>(name);
    }
}
