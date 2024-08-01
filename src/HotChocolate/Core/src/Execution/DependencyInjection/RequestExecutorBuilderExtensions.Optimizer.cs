using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Processing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddOperationCompilerOptimizer<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOperationCompilerOptimizer
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchemaServices(s => s.AddSingleton<IOperationCompilerOptimizer, T>());
        return builder;
    }

    public static IRequestExecutorBuilder AddOperationCompilerOptimizer<T>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IOperationCompilerOptimizer
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchemaServices(
            sc => sc.AddSingleton<IOperationCompilerOptimizer>(
                sp => factory(sp.GetCombinedServices())));
        return builder;
    }
}
