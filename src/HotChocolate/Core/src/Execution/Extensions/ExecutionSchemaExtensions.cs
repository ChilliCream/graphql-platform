using System;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public static class ExecutionSchemaExtensions
{
    public static IRequestExecutor MakeExecutable(
        this ISchema schema)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        return new ServiceCollection()
            .AddGraphQL()
            .Configure(o => o.Schema = schema)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync()
            .Result;
    }

    public static IRequestExecutor MakeExecutable(
        this ISchema schema,
        RequestExecutorOptions options)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new ServiceCollection()
            .AddGraphQL()
            .Configure(o => o.Schema = schema)
            .SetRequestOptions(() => options)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync()
            .Result;
    }

    public static bool IsRootType(this ISchema schema, IType type)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.IsObjectType())
        {
            return IsType(schema.QueryType, type)
                || IsType(schema.MutationType, type)
                || IsType(schema.SubscriptionType, type);
        }
        return false;
    }

    private static bool IsType(ObjectType? left, IType right)
    {
        if (left is null)
        {
            return false;
        }

        return ReferenceEquals(left, right);
    }
}
