using System;
using System.Collections.Generic;
using GreenDonut;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddTypeDiscoveryHandler<T>(
        this IRequestExecutorBuilder builder,
        Func<IDescriptorContext, T> factory)
        where T : TypeDiscoveryHandler
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        builder.ConfigureSchema(
            b => b.SetContextData(
                WellKnownContextData.TypeDiscoveryHandlers,
                value =>
                {
                    if (value is not List<Func<IDescriptorContext, TypeDiscoveryHandler>> list)
                    {
                        list = new List<Func<IDescriptorContext, TypeDiscoveryHandler>>();
                    }

                    list.Add(factory);
                    return list;
                }));

        return builder;
    }
}
