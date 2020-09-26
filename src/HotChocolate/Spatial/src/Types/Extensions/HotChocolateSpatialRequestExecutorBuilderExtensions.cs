using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateSpatialRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddSpatialTypes(
            this IRequestExecutorBuilder builder)
        {
            return builder.ConfigureSchema(x => x.AddSpatialTypes());
        }
    }
}
