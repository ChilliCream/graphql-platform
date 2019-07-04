using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Properties;
using HotChocolate.Resolvers.Expressions.Parameters;

namespace HotChocolate.Resolvers.Expressions
{
    internal sealed class ResolverCompiler
    {
        private static readonly MethodInfo _awaitHelper =
            typeof(ExpressionHelper).GetMethod("AwaitHelper");
        private static readonly MethodInfo _wrapResultHelper =
            typeof(ExpressionHelper).GetMethod("WrapResultHelper");
        private static readonly MethodInfo _parent =
            typeof(IResolverContext).GetMethod("Parent");
        private static readonly MethodInfo _resolver =
            typeof(IResolverContext).GetMethod("Resolver");

        private readonly IResolverParameterCompiler[] _compilers;
        private readonly ParameterExpression _context;
        private readonly MethodInfo _taskResult;

        public ResolverCompiler()
            : this(ParameterCompilerFactory.Create())
        {
        }

        public ResolverCompiler(
            IEnumerable<IResolverParameterCompiler> compilers)
        {
            if (compilers == null)
            {
                throw new ArgumentNullException(nameof(compilers));
            }

            _compilers = compilers.ToArray();

            Type contextType = typeof(IResolverContext);
            TypeInfo contextTypeInfo = contextType.GetTypeInfo();

            _context = Expression.Parameter(contextType);

            _taskResult = typeof(Task)
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(Task.FromResult));
            _taskResult = _taskResult.MakeGenericMethod(typeof(object));
        }

        public FieldResolver Compile(ResolverDescriptor descriptor)
        {
            MethodInfo resolverMethod = descriptor.ResolverType is null
                ? _parent.MakeGenericMethod(descriptor.SourceType)
                : _resolver.MakeGenericMethod(descriptor.ResolverType);

            Expression resolverInstance = Expression.Call(
                _context, resolverMethod);

            FieldResolverDelegate resolver = CreateResolver(
                resolverInstance,
                descriptor.Field.Member,
                descriptor.SourceType);

            return new FieldResolver(
                descriptor.Field.TypeName,
                descriptor.Field.FieldName,
                resolver);
        }

        private FieldResolverDelegate CreateResolver(
            Expression resolverInstance,
            MemberInfo member,
            Type sourceType)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member is MethodInfo method)
            {
                IEnumerable<Expression> parameters = CreateParameters(
                    method.GetParameters(), sourceType);

                MethodCallExpression resolverExpression =
                    Expression.Call(resolverInstance, method, parameters);

                Expression handleResult = HandleResult(
                    resolverExpression, method.ReturnType);

                return Expression.Lambda<FieldResolverDelegate>(
                    handleResult, _context).Compile();
            }

            if (member is PropertyInfo property)
            {
                MemberExpression propertyAccessor = Expression.Property(
                    resolverInstance, property);

                Expression handleResult = HandleResult(
                    propertyAccessor, property.PropertyType);

                return Expression.Lambda<FieldResolverDelegate>(
                    handleResult, _context).Compile();
            }

            throw new NotSupportedException();
        }

        private IEnumerable<Expression> CreateParameters(
            IEnumerable<ParameterInfo> parameters,
            Type sourceType)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                IResolverParameterCompiler parameterCompiler =
                    _compilers.FirstOrDefault(t =>
                        t.CanHandle(parameter, sourceType));

                if (parameterCompiler == null)
                {
                    throw new InvalidOperationException(
                        TypeResources.ResolverCompiler_UnknownParameterType);
                }

                yield return parameterCompiler.Compile(
                    _context, parameter, sourceType);
            }
        }

        private static Expression HandleResult(
            Expression resolverExpression,
            Type resultType)
        {
            if (resultType == typeof(Task<object>))
            {
                return resolverExpression;
            }
            else if (typeof(Task).IsAssignableFrom(resultType)
                && resultType.IsGenericType)
            {
                return AwaitMethodCall(
                    resolverExpression,
                    resultType.GetGenericArguments().First());
            }
            else
            {
                return WrapResult(
                    resolverExpression,
                    resultType);
            }
        }

        private static MethodCallExpression AwaitMethodCall(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitHelper.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression WrapResult(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper =
                _wrapResultHelper.MakeGenericMethod(valueType);
            return Expression.Call(wrapResultHelper, taskExpression);
        }
    }

    internal static class ExpressionHelper
    {
        public static async Task<object> AwaitHelper<T>(Task<T> task)
        {
            if (task == null)
            {
                return null;
            }
            return await task;
        }

        public static Task<object> WrapResultHelper<T>(T result)
        {
            return Task.FromResult<object>(result);
        }

        public static TContextData ResolveContextData<TContextData>(
            IDictionary<string, object> contextData,
            string key,
            bool defaultIfNotExists)
        {
            if (contextData.TryGetValue(key, out object value))
            {
                if (value is null)
                {
                    return default;
                }

                if (value is TContextData v)
                {
                    return v;
                }
            }
            else if (defaultIfNotExists)
            {
                return default;
            }

            // TODO : resources
            throw new ArgumentException(
                "The specified context key does not exist.");
        }

        public static TContextData ResolveScopedContextData<TContextData>(
            IReadOnlyDictionary<string, object> contextData,
            string key,
            bool defaultIfNotExists)
        {
            if (contextData.TryGetValue(key, out object value))
            {
                if (value is null)
                {
                    return default;
                }

                if (value is TContextData v)
                {
                    return v;
                }
            }
            else if (defaultIfNotExists)
            {
                return default;
            }

            // TODO : resources
            throw new ArgumentException(
                "The specified context key does not exist.");
        }
    }
}
