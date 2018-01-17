using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus
{
    public delegate Task<object> ResolveAsync(IServiceProvider serviceProvider, IResolverContext context, CancellationToken cancellationToken);

    public interface IResolverBuilder
    {
        IResolverBuilder Add(string typeName, string fieldName, Func<IServiceProvider, IResolverContext, object> resolver);
        IResolverBuilder Add(string typeName, string fieldName, Func<IServiceProvider, IResolverContext, CancellationToken, Task<object>> resolver);

        IResolverCollection Build();
    }

    public static class ResolverBuilderExtensions
    {

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder, string typeName, string fieldName, Func<IResolverContext, object> resolver)
        {
            return resolverBuilder.Add(typeName, fieldName, (sp, rc) => resolver(rc));
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder, string typeName, string fieldName, Func<IResolverContext, CancellationToken, Task<object>> resolver)
        {
            return resolverBuilder.Add(typeName, fieldName, (sp, rc, ct) => resolver(rc, ct));
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder, string typeName, string fieldName, Func<object> resolver)
        {
            resolverBuilder.Add(typeName, fieldName, rc => resolver());
            return resolverBuilder;
        }

        public static IResolverBuilder Add<TResolver>(this IResolverBuilder resolverBuilder, string typeName, string fieldName, Func<IResolverContext, Task<object>> resolver)
        {
            resolverBuilder.Add(typeName, fieldName, (rc, ct) => resolver(rc));
            return resolverBuilder;
        }

        public static IResolverBuilder Add<TResolver>(this IResolverBuilder resolverBuilder, string typeName, string fieldName, Func<CancellationToken, Task<object>> resolver)
        {
            resolverBuilder.Add(typeName, fieldName, (rc, ct) => resolver(ct));
            return resolverBuilder;
        }

        public static IResolverBuilder Add<TResolver>(this IResolverBuilder resolverBuilder, string typeName, string fieldName, Func<Task<object>> resolver)
        {
            resolverBuilder.Add(typeName, fieldName, (rc, ct) => resolver());
            return resolverBuilder;
        }

        public static IResolverBuilder Add<TResolver>(this IResolverBuilder resolverBuilder, string typeName, string fieldName)
            where TResolver : IResolver
        {
            resolverBuilder.Add(typeName, fieldName, (sp, rc, ct) =>
            {
                return sp.GetService<TResolver>().ResolveAsync(rc, ct);
            });
            return resolverBuilder;
        }

        public static IResolverBuilder Add<TResolver>(this IResolverBuilder resolverBuilder, string typeName, string fieldName, TResolver resolver)
            where TResolver : IResolver
        {
            resolverBuilder.Add(typeName, fieldName, (rc, ct) =>
            {
                return resolver.ResolveAsync(rc, ct);
            });
            return resolverBuilder;
        }

        public static IResolverBuilder Add<TFieldResolver>(this IResolverBuilder resolverBuilder, TFieldResolver resolver)
            where TFieldResolver : IFieldResolver
        {
            resolverBuilder.Add(resolver.TypeName, resolver.FieldName, (rc, ct) =>
            {
                return resolver.ResolveAsync(rc, ct);
            });
            return resolverBuilder;
        }

        public static IResolverBuilder Add<T>(this IResolverBuilder resolverBuilder, Expression<Func<T, object>> field, Func<IResolverContext, object> resolver)
        {
            throw new NotImplementedException();
        }

        public static IResolverBuilder Add<T>(this IResolverBuilder resolverBuilder, Func<IResolverContext, CancellationToken, Task<object>> resolver)
        {
            throw new NotImplementedException();
        }



        private static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }
    }
}
