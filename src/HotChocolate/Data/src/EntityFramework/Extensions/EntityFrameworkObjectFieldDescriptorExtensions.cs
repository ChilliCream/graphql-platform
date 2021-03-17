using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Data;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;

namespace HotChocolate.Types
{
    public static class EntityFrameworkObjectFieldDescriptorExtensions
    {
        private static Type _valueTask = typeof(ValueTask<>);
        private static Type _task = typeof(Task<>);

        public static IObjectFieldDescriptor UseDbContext<TDbContext>(
            this IObjectFieldDescriptor descriptor)
            where TDbContext : DbContext
        {
            string scopedServiceName = typeof(TDbContext).FullName ?? typeof(TDbContext).Name;
            FieldMiddleware placeholder = next => context => throw new NotSupportedException();

            descriptor
                .Use(next => async context =>
                {
                    await using TDbContext dbContext = context.Services
                        .GetRequiredService<IDbContextFactory<TDbContext>>()
                        .CreateDbContext();

                    try
                    {
                        context.SetLocalValue(scopedServiceName, dbContext);
                        await next(context).ConfigureAwait(false);
                    }
                    finally
                    {
                        context.RemoveLocalValue(scopedServiceName);
                    }
                })
                .Use(placeholder)
                .Extend()
                .OnBeforeNaming((c, d) =>
                {
                    if (d.ResultType is null)
                    {
                        d.MiddlewareComponents.Remove(placeholder);
                        return;
                    }

                    if (TryExtractEntityType(d.ResultType, out Type? entityType))
                    {
                        Type middleware = typeof(ToListMiddleware<>).MakeGenericType(entityType);
                        var index = d.MiddlewareComponents.IndexOf(placeholder);
                        d.MiddlewareComponents[index] = Create(middleware);
                        return;
                    }

                    if (IsExecutable(d.ResultType))
                    {
                        Type middleware = typeof(ExecutableMiddleware);
                        var index = d.MiddlewareComponents.IndexOf(placeholder);
                        d.MiddlewareComponents[index] = Create(middleware);
                    }

                    d.MiddlewareComponents.Remove(placeholder);
                });

            return descriptor;
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

            Type resultTypeDefinition = resultType.GetGenericTypeDefinition();
            if ((resultTypeDefinition == _task || resultTypeDefinition == _valueTask) &&
                typeof(IExecutable).IsAssignableFrom(resultType.GenericTypeArguments[0]))
            {
                return true;
            }

            return false;
        }
    }
}
