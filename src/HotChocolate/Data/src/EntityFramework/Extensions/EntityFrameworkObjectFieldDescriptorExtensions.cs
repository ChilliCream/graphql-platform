using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Data;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;

namespace HotChocolate.Types;

public static class EntityFrameworkObjectFieldDescriptorExtensions
{
    private static readonly Type _valueTask = typeof(ValueTask<>);
    private static readonly Type _task = typeof(Task<>);

    public static IObjectFieldDescriptor UseDbContext<TDbContext>(
        this IObjectFieldDescriptor descriptor)
        where TDbContext : DbContext
    {
        var scopedServiceName = typeof(TDbContext).FullName ?? typeof(TDbContext).Name;
        FieldMiddlewareDefinition placeholder =
            new(_ => _ => throw new NotSupportedException(), key: WellKnownMiddleware.ToList);

        descriptor.Extend().Definition.MiddlewareDefinitions.Add(
            new(next => async context =>
            {
#if NET6_0_OR_GREATER
                await using var dbContext = await context.RequestServices
                    .GetRequiredService<IDbContextFactory<TDbContext>>()
                    .CreateDbContextAsync()
                    .ConfigureAwait(false);
#else
                using TDbContext dbContext = context.RequestServices
                    .GetRequiredService<IDbContextFactory<TDbContext>>()
                    .CreateDbContext();
#endif

                try
                {
                    context.SetLocalState(scopedServiceName, dbContext);
                    await next(context).ConfigureAwait(false);
                }
                finally
                {
                    context.RemoveLocalState(scopedServiceName);
                }
            }, key: WellKnownMiddleware.DbContext));

        descriptor.Extend().Definition.MiddlewareDefinitions.Add(placeholder);

        descriptor
            .Extend()
            .OnBeforeNaming((_, d) => AddCompletionMiddleware(d, placeholder));

        return descriptor;
    }

    internal static void UseDbContext<TDbContext>(
        ObjectFieldDefinition definition)
        where TDbContext : DbContext
    {
        var scopedServiceName = typeof(TDbContext).FullName ?? typeof(TDbContext).Name;

        FieldMiddlewareDefinition placeholderMiddleware =
            new(_ => _ => throw new NotSupportedException(), key: WellKnownMiddleware.ToList);

        FieldMiddlewareDefinition contextMiddleware =
            new(next => async context =>
                {
                    var dbContext = await context.RequestServices
                        .GetRequiredService<IDbContextFactory<TDbContext>>()
                        .CreateDbContextAsync()
                        .ConfigureAwait(false);

                    context.RegisterForCleanup(() => dbContext.DisposeAsync());

                    try
                    {
                        context.SetLocalState(scopedServiceName, dbContext);
                        await next(context).ConfigureAwait(false);
                    }
                    finally
                    {
                        context.RemoveLocalState(scopedServiceName);
                    }
                },
                key: WellKnownMiddleware.DbContext);

        definition.MiddlewareDefinitions.Insert(0, placeholderMiddleware);
        definition.MiddlewareDefinitions.Insert(0, contextMiddleware);

        AddCompletionMiddleware(definition, placeholderMiddleware);
    }

    internal static void AddCompletionMiddleware(
        ObjectFieldDefinition definition,
        FieldMiddlewareDefinition placeholderMiddleware)
    {
        if (definition.ResultType is null)
        {
            definition.MiddlewareDefinitions.Remove(placeholderMiddleware);
            return;
        }

        if (TryExtractEntityType(definition.ResultType, out Type? entityType))
        {
            Type middleware = typeof(ToListMiddleware<>).MakeGenericType(entityType);
            var index = definition.MiddlewareDefinitions.IndexOf(placeholderMiddleware);
            definition.MiddlewareDefinitions[index] =
                new(Create(middleware), key: WellKnownMiddleware.ToList);
            return;
        }

        if (IsExecutable(definition.ResultType))
        {
            Type middleware = typeof(ExecutableMiddleware);
            var index = definition.MiddlewareDefinitions.IndexOf(placeholderMiddleware);
            definition.MiddlewareDefinitions[index] =
                new(Create(middleware), key: WellKnownMiddleware.ToList);
        }

        definition.MiddlewareDefinitions.Remove(placeholderMiddleware);
    }

    private static bool TryExtractEntityType(
        Type resultType,
        [NotNullWhen(true)] out Type? entityType)
    {
        if (!resultType.IsGenericType)
        {
            entityType = null;
            return false;
        }

        if (typeof(IEnumerable).IsAssignableFrom(resultType))
        {
            entityType = resultType.GenericTypeArguments[0];
            return true;
        }

        Type resultTypeDefinition = resultType.GetGenericTypeDefinition();
        if ((resultTypeDefinition == _task || resultTypeDefinition == _valueTask) &&
            typeof(IEnumerable).IsAssignableFrom(resultType.GenericTypeArguments[0]) &&
            resultType.GenericTypeArguments[0].IsGenericType)
        {
            entityType = resultType.GenericTypeArguments[0].GenericTypeArguments[0];
            return true;
        }

        entityType = null;
        return false;
    }

    private static bool IsExecutable(Type resultType)
    {
        if (typeof(IExecutable).IsAssignableFrom(resultType))
        {
            return true;
        }

        if (!resultType.IsGenericType)
        {
            return false;
        }

        var resultTypeDefinition = resultType.GetGenericTypeDefinition();
        return (resultTypeDefinition == _task || resultTypeDefinition == _valueTask) &&
            typeof(IExecutable).IsAssignableFrom(resultType.GenericTypeArguments[0]);
    }
}
