using System;
using System.Linq;
using HotChocolate.Data;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;

namespace HotChocolate.Types
{
    public static class EntityFrameworkObjectFieldDescriptorExtensions
    {
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
                    if (d.ResultType is not null &&
                        typeof(IQueryable).IsAssignableFrom(d.ResultType) &&
                        d.ResultType.IsGenericType)
                    {
                        Type entity = d.ResultType.GenericTypeArguments[0];
                        Type middleware = typeof(ToListMiddleware<>).MakeGenericType(entity);

                        var index = d.MiddlewareComponents.IndexOf(placeholder);
                        d.MiddlewareComponents[index] = Create(middleware);

                    }
                    else if (d.ResultType is not null &&
                        typeof(IExecutable).IsAssignableFrom(d.ResultType))
                    {
                        Type middleware = typeof(ExecuableMiddleware);

                        var index = d.MiddlewareComponents.IndexOf(placeholder);
                        d.MiddlewareComponents[index] = Create(middleware);
                    }
                    else
                    {
                        d.MiddlewareComponents.Remove(placeholder);
                    }
                });

            return descriptor;
        }
    }
}
