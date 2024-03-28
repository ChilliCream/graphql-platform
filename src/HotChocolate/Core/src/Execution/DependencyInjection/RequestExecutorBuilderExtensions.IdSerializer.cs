using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddNodeIdValueSerializer<T>(
        this IRequestExecutorBuilder builder)
        where T : class, INodeIdValueSerializer
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.ConfigureSchemaServices(
            services => services.AddSingleton<INodeIdValueSerializer, T>());
        return builder;
    }
}
