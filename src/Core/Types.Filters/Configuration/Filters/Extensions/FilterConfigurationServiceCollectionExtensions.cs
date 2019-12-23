using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Types.Filters.Configuration
{
    public static class FilterConfigurationServiceCollectionExtensions
    {
        public static IFilterConfigurationBuilder AddOperationClientOptions(
          this IServiceCollection services)
        {
            services.AddOptions();
            services.TryAddSingleton<IFilterOptions, FilterOptions>();
            return new DefaultFilterConfigurationBuilder(services);
        }
    }
}
