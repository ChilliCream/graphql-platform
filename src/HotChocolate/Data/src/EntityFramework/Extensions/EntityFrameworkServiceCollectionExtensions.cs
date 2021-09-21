using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Extensions
{
    public static class EntityFrameworkServiceCollectionExtensions
    {
        // todo: get rid of this
        public static IServiceCollection AddDbContextInjection(
            this IServiceCollection services)
        {
            return services.AddSingleton<IParameterExpressionBuilder, DbContextParameterExpressionBuilder>();
        }
    }
}
