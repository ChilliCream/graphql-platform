using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Descriptors
{
    public static class ConventionExtension
    {
        public static IServiceCollection AddConvention<TConvention, TConcreteConvention>(
           this IServiceCollection services)
           where TConvention : IConvention
           where TConcreteConvention : IConvention
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddSingleton(
                new ConventionRecord(
                    typeof(TConvention),
                    (s) => s.GetRequiredService<TConcreteConvention>()
                    )
                );
        }

        public static IServiceCollection AddConvention<TConvention>(
           this IServiceCollection services, IConvention convention)
           where TConvention : IConvention
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            return services.AddSingleton(
                new ConventionRecord(
                    typeof(TConvention),
                    (s) => convention
                    )
                );
        }
    }
}
