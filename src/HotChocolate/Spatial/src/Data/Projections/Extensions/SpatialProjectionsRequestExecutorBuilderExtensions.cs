using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public static class SpatialProjectionsRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddSpatialProjections(
        this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(x => x.AddSpatialProjections());
    }
}
