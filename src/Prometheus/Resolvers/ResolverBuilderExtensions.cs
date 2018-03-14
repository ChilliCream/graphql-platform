using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    public static class ResolverBuilderExtensions
    {
        public static IResolverBuilder Add<TResolver>(
            this IResolverBuilder resolverBuilder,
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

            return resolverBuilder.Add((s, sp, reg) =>
            {
                IResolver internalResolver = sp.GetService<TResolver>();
                ResolverDelegate resolver = new ResolverDelegate(
                    (ctx, ct) => internalResolver.ResolveAsync(ctx, ct));
                reg(typeName, fieldName, resolver);
            });
        }

        public static IResolverBuilder Add(
            this IResolverBuilder resolverBuilder,
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

            return resolverBuilder.Add((s, sp, reg) =>
            {
                reg(typeName, fieldName, resolver);
            });
        }

        public static IResolverBuilder Add(
            this IResolverBuilder resolverBuilder,
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

            ResolverDelegate resolver = new ResolverDelegate(
                (ctx, ct) => resolverFunc(ctx));

            return ResolverBuilderExtensions.Add(resolverBuilder,
                typeName, fieldName, resolver);
        }

        public static IResolverBuilder Add(
            this IResolverBuilder resolverBuilder,
            string typeName, string fieldName,
            Func<Task<object>> resolverFunc)
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

            ResolverDelegate resolver = new ResolverDelegate(
                (ctx, ct) => resolverFunc());

            return ResolverBuilderExtensions.Add(resolverBuilder,
                typeName, fieldName, resolver);
        }

        public static IResolverBuilder Add(
            this IResolverBuilder resolverBuilder,
            string typeName, string fieldName,
            Func<IResolverContext, object> resolverFunc)
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

            ResolverDelegate resolver = new ResolverDelegate(
                (ctx, ct) => Task.FromResult(resolverFunc(ctx)));

            return ResolverBuilderExtensions.Add(resolverBuilder,
                typeName, fieldName, resolver);
        }

        public static IResolverBuilder Add(
            this IResolverBuilder resolverBuilder,
            string typeName, string fieldName,
            Func<object> resolverFunc)
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

            ResolverDelegate resolver = new ResolverDelegate(
                (ctx, ct) => Task.FromResult(resolverFunc()));

            return ResolverBuilderExtensions.Add(resolverBuilder,
                typeName, fieldName, resolver);
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

        public static IResolverBuilder AddQueryType<TQuery>(
            this IResolverBuilder resolverBuilder)
            where TQuery : class
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            AddResolverType<TQuery>(resolverBuilder,
                OperationType.Query.ToString(), null);
            return resolverBuilder;
        }

        public static IResolverBuilder AddQueryType<TQuery>(
            this IResolverBuilder resolverBuilder,
            TQuery query)
            where TQuery : class
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            AddResolverType(resolverBuilder,
                OperationType.Query.ToString(), query);
            return resolverBuilder;
        }

        public static IResolverBuilder AddMutationType<TMutation>(
            this IResolverBuilder resolverBuilder)
            where TMutation : class
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            AddResolverType<TMutation>(resolverBuilder,
                OperationType.Query.ToString(), null);
            return resolverBuilder;
        }

        public static IResolverBuilder AddMutationType<TMutation>(
            this IResolverBuilder resolverBuilder,
            TMutation mutation)
            where TMutation : class
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (mutation == null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            AddResolverType(resolverBuilder,
                OperationType.Query.ToString(), mutation);
            return resolverBuilder;
        }

        public static IResolverBuilder AddType<T>(
            this IResolverBuilder resolverBuilder)
            where T : class
        {
            string typeName = GetTypeName(typeof(T));
            return AddType<T>(resolverBuilder, typeName);
        }

        public static IResolverBuilder AddType<T>(
            this IResolverBuilder resolverBuilder,
            T instance)
            where T : class
        {
            string typeName = GetTypeName(typeof(T));
            return AddType<T>(resolverBuilder, typeName, instance);
        }

        public static IResolverBuilder AddType<T>(
            this IResolverBuilder resolverBuilder,
            string typeName)
            where T : class
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(
                    "The type name mustn't be null or empty.",
                    nameof(typeName));
            }

            AddResolverType<T>(resolverBuilder, typeName, null);
            return resolverBuilder;
        }

        public static IResolverBuilder AddType<T>(
            this IResolverBuilder resolverBuilder,
            string typeName,
            T instance)
            where T : class
        {
            if (resolverBuilder == null)
            {
                throw new ArgumentNullException(nameof(resolverBuilder));
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException(
                    "The type name mustn't be null or empty.",
                    nameof(typeName));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            AddResolverType(resolverBuilder, typeName, instance);
            return resolverBuilder;
        }

        private static void AddResolverType<T>(IResolverBuilder resolverBuilder,
            string typeName, T resolverTypeInstance)
            where T : class
        {
            resolverBuilder.Add((s, sp, reg) =>
            {
                if (s.ObjectTypes.TryGetValue(typeName, out var typeDefinition))
                {
                    ReflectionHelper reflectionHelper = resolverTypeInstance == null
                        ? new ReflectionHelper(typeof(T), sp.GetService(typeof(T)))
                        : new ReflectionHelper(typeof(T), resolverTypeInstance);

                    foreach (FieldDefinition field in typeDefinition.Fields.Values)
                    {
                        if (reflectionHelper.TryGetResolver(field.Name, out var resolver))
                        {
                            reg(typeName, field.Name, resolver);
                        }
                    }
                }
            });
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

        private static string GetTypeName(Type type)
        {
            if (type.IsDefined(typeof(GraphQLNameAttribute)))
            {
                return type.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }
            return type.Name;
        }
    }
}