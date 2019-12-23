using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Filters.Configuration.Filters.Extensions
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IOperationClientBuilder"/>
    /// </summary>
    public static class FilterConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the filter options that will be used to create the filter with
        /// `.UseFiltering()`
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configure">
        /// A delegate that is used to configure the <see cref="FilterOptionsModifiers"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IFilterConfigurationBuilder"/> that can be used to configure the filters.
        /// </returns>
        public static IFilterConfigurationBuilder ConfigureFilters(
            this IFilterConfigurationBuilder builder,
            Action<FilterOptionsModifiers> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure<FilterOptionsModifiers>(
                options => configure(options));

            return builder;
        }

        public static IFilterConfigurationBuilder AddImplicitFilter(
            this IFilterConfigurationBuilder builder,
            int operationKind,
            TryCreateImplicitFilter filterFactory) =>
            builder.ConfigureFilters((s) =>
                s.InfereFilters.Add(infereFilters =>
                {
                    infereFilters[operationKind] = filterFactory;
                }));

        public static IFilterConfigurationBuilder Ignore(
            this IFilterConfigurationBuilder builder,
            int operationKind) =>
            builder.ConfigureFilters((s) =>
                s.InfereFilters.Add(infereFilters =>
                {
                    infereFilters.Remove(operationKind);
                }));
    }
}
