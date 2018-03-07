using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{
    public static class ResolverBuilderExtensions
    {
        public static IResolverBuilder Add<TResolver>(this IResolverBuilder resolverBuilder,
            string typeName, string fieldName)
            where TResolver : IResolver
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            return resolverBuilder.Add(typeName, fieldName, sp =>
            {
                IResolver resolver = sp.GetService<TResolver>();
                return new ResolverDelegate((ctx, ct) => resolver.ResolveAsync(ctx, ct));
            });
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
            string typeName, string fieldName,
            ResolverDelegate resolver)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return resolverBuilder.Add(typeName, fieldName, sp => resolver);
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
            string typeName, string fieldName,
            Func<IResolverContext, Task<object>> resolverFunc)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            return resolverBuilder.Add(typeName, fieldName,
                sp => new ResolverDelegate((ctx, ct) => resolverFunc(ctx)));
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
            string typeName, string fieldName, Func<Task<object>> resolverFunc)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            return resolverBuilder.Add(typeName, fieldName,
                sp => new ResolverDelegate((ctx, ct) => resolverFunc()));
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
            string typeName, string fieldName, Func<IResolverContext, object> resolverFunc)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            return resolverBuilder.Add(typeName, fieldName,
                sp => new ResolverDelegate((ctx, ct) => Task.FromResult(resolverFunc(ctx))));
        }

        public static IResolverBuilder Add(this IResolverBuilder resolverBuilder,
            string typeName, string fieldName, Func<object> resolverFunc)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            return resolverBuilder.Add(typeName, fieldName,
                sp => new ResolverDelegate((ctx, ct) => Task.FromResult(resolverFunc())));
        }

        public static IResolverBuilder Add<TResolver, TSource>(
            this IResolverBuilder resolverBuilder,
            Expression<Func<TSource, object>> propertyExpression)
            where TResolver : IResolver
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (propertyExpression == null)
            {
                throw new ArgumentNullException(nameof(propertyExpression));
            }

            FieldReference fieldReference = GetFieldReference(propertyExpression);
            return ResolverBuilderExtensions.Add<TResolver>(resolverBuilder,
                fieldReference.TypeName, fieldReference.FieldName);
        }

        public static IResolverBuilder Add<TSource>(
            this IResolverBuilder resolverBuilder,
            Expression<Func<TSource, object>> propertyExpression,
            ResolverDelegate resolver)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (propertyExpression == null)
            {
                throw new ArgumentNullException(nameof(propertyExpression));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            FieldReference fieldReference = GetFieldReference(propertyExpression);
            return ResolverBuilderExtensions.Add(resolverBuilder,
               fieldReference.TypeName, fieldReference.FieldName,
               resolver);
        }

        public static IResolverBuilder Add<TSource>(
            this IResolverBuilder resolverBuilder,
            Expression<Func<TSource, object>> propertyExpression,
            Func<IResolverContext, Task<object>> resolverFunc)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (propertyExpression == null)
            {
                throw new ArgumentNullException(nameof(propertyExpression));
            }

            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            FieldReference fieldReference = GetFieldReference(propertyExpression);
            return ResolverBuilderExtensions.Add(resolverBuilder,
                fieldReference.TypeName, fieldReference.FieldName,
                resolverFunc);
        }

        public static IResolverBuilder Add<TSource>(
            this IResolverBuilder resolverBuilder,
            Expression<Func<TSource, object>> propertyExpression,
            Func<Task<object>> resolverFunc)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (propertyExpression == null)
            {
                throw new ArgumentNullException(nameof(propertyExpression));
            }

            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            FieldReference fieldReference = GetFieldReference(propertyExpression);
            return ResolverBuilderExtensions.Add(resolverBuilder,
                fieldReference.TypeName, fieldReference.FieldName,
                resolverFunc);
        }

        public static IResolverBuilder Add<TSource>(
            this IResolverBuilder resolverBuilder,
            Expression<Func<TSource, object>> propertyExpression,
            Func<IResolverContext, object> resolverFunc)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (propertyExpression == null)
            {
                throw new ArgumentNullException(nameof(propertyExpression));
            }

            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            FieldReference fieldReference = GetFieldReference(propertyExpression);
            return ResolverBuilderExtensions.Add(resolverBuilder,
                fieldReference.TypeName, fieldReference.FieldName,
                resolverFunc);
        }

        public static IResolverBuilder Add<TSource, TProperty>(
            this IResolverBuilder resolverBuilder,
            Expression<Func<TSource, object>> propertyExpression,
            Func<object> resolverFunc)
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (propertyExpression == null)
            {
                throw new ArgumentNullException(nameof(propertyExpression));
            }

            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            FieldReference fieldReference = GetFieldReference(propertyExpression);
            return ResolverBuilderExtensions.Add(resolverBuilder,
                fieldReference.TypeName, fieldReference.FieldName,
                resolverFunc);
        }

        private static FieldReference GetFieldReference<TSource>(
            Expression<Func<TSource, object>> propertyExpression)
        {
            Type type = typeof(TSource);

            if (propertyExpression.Body is MemberExpression m
                && m.Member is PropertyInfo p
                && type == p.ReflectedType)
            {
                string fieldName = p.Name.Substring(0, 1).ToLowerInvariant() + p.Name.Substring(1);
                return FieldReference.Create(type.Name, fieldName);
            }

            throw new ArgumentException("The specied expression does not refer to a property.", nameof(propertyExpression));
        }

        private static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }
    }
}