using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
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
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddMongoDbFiltering(
            this IRequestExecutorBuilder builder) =>
            builder.ConfigureSchema(s => s.AddFiltering(x => x.UseDefaults())

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
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddMonogDbFilter(configure, name));

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
        public static IRequestExecutorBuilder AddMongoDbFilter<TConvention>(
            this IRequestExecutorBuilder builder,
            string? name = null)
            where TConvention : class, IFilterConvention =>
            builder.ConfigureSchema(s => s.AddFiltering<TConvention>(name));

         /// <summary>
        /// Adds filtering support.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        public static IRequestExecutorBuilder AddSorting(
            this IRequestExecutorBuilder builder) =>
            builder.ConfigureSchema(s => s.AddSorting());

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
        public static IRequestExecutorBuilder AddSorting(
            this IRequestExecutorBuilder builder,
            Action<ISortConventionDescriptor> configure,
            string? name = null) =>
            builder.ConfigureSchema(s => s.AddSorting(configure, name));

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
        public static IRequestExecutorBuilder AddSorting<TConvention>(
            this IRequestExecutorBuilder builder,
            string? name = null)
            where TConvention : class, ISortConvention =>
            builder.ConfigureSchema(s => s.AddSorting<TConvention>(name));
    }
}
