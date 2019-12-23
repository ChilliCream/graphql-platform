using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Filters.Configuration
{
    /// <summary>
    /// A builder for configuring default behavior of filters
    /// </summary>
    public interface IFilterConfigurationBuilder
    {
        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
