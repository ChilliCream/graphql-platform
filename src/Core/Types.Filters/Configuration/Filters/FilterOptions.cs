using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;

namespace HotChocolate.Types.Filters.Configuration
{
    public class FilterOptions : IFilterOptions
    {
        private FilterConfiguration configuration;
        private readonly IOptionsMonitor<FilterOptionsModifiers> _optionsMonitor;

        public FilterOptions(IOptionsMonitor<FilterOptionsModifiers> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor
                ?? throw new ArgumentNullException(nameof(optionsMonitor));
        }

        public IEnumerable<TryCreateImplicitFilter> GetImplicitFilterFactories()
        {
            return GetOrCreateConfiguration().ImplicitFilters.Values;
        }

        private FilterConfiguration GetOrCreateConfiguration()
        {
            lock (configuration)
            {
                if (configuration == null)
                {
                    configuration = CreateConfiguration();
                }
                return configuration;
            }
        }
        private FilterConfiguration CreateConfiguration()
        {
            FilterOptionsModifiers options = _optionsMonitor.CurrentValue;
            var configuration = new FilterConfiguration();

            foreach (ConfigureFilterOperation configure in options.InfereFilters)
            {
                configure(configuration.ImplicitFilters);
            }

            return configuration;
        }

    }
}
