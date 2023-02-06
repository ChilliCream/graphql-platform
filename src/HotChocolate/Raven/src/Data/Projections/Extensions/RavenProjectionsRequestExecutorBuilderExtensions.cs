using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public static class RavenProjectionsRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddRavenProjections(
        this IRequestExecutorBuilder requestExecutorBuilder)
        => requestExecutorBuilder.ConfigureSchema(x => x.AddRavenProjections());
}
