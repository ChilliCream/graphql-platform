using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public static class SpatialFilteringRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddSpatialFiltering(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(x => x.AddSpatialFiltering());
    }
}
