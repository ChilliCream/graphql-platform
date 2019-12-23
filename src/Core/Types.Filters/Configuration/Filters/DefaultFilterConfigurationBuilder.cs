using System;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Filters.Configuration
{
    internal class DefaultFilterConfigurationBuilder : IFilterConfigurationBuilder
    {
        public DefaultFilterConfigurationBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
