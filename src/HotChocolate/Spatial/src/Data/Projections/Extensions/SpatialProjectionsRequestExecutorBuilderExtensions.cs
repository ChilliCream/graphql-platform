using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Spatial;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate
{
    public static class SpatialProjectionsRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddSpatialProjections(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(x => x.AddSpatialProjections());
        }
    }
}
