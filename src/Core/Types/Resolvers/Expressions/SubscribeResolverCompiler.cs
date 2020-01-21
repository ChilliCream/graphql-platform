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
    internal sealed class SubscribeResolverCompiler
    {
        private static readonly MethodInfo _awaitAsyncEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitAsyncEnumerable));
        private static readonly MethodInfo _awaitEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitEnumerable));
        private static readonly MethodInfo _awaitQueryable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitQueryable));
        private static readonly MethodInfo _awaitObservable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitObservable));
        private static readonly MethodInfo _wrapAsyncEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapAsyncEnumerable));
        private static readonly MethodInfo _wrapEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapEnumerable));
        private static readonly MethodInfo _wrapQueryable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapQueryable));
        private static readonly MethodInfo _wrapObservable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapObservable));
        private static readonly MethodInfo _parent =
            typeof(IResolverContext).GetMethod("Parent");
        private static readonly MethodInfo _resolver =
            typeof(IResolverContext).GetMethod("Resolver");

        private readonly IResolverParameterCompiler[] _compilers;
        private readonly ParameterExpression _context;
        private readonly MethodInfo _taskResult;

        public SubscribeResolverCompiler()
            : this(ParameterCompilerFactory.Create())
        {
        }

        public SubscribeResolverCompiler(
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

        public static SubscribeResolverCompiler Default { get; } =
            new SubscribeResolverCompiler();

        public SubscribeResolverDelegate Compile(ResolverDescriptor descriptor)
        {
            MethodInfo resolverMethod = descriptor.ResolverType is null
                ? _parent.MakeGenericMethod(descriptor.SourceType)
                : _resolver.MakeGenericMethod(descriptor.ResolverType);

            Expression resolverInstance = Expression.Call(
                _context, resolverMethod);

            return CreateResolver(
                resolverInstance,
                descriptor.Field.Member,
                descriptor.SourceType);
        }

        private SubscribeResolverDelegate CreateResolver(
            Expression resolverInstance,
            MemberInfo member,
            Type sourceType)
        {
            if (member is MethodInfo method)
            {
                IEnumerable<Expression> parameters = CreateParameters(
                    method.GetParameters(), sourceType);

                MethodCallExpression resolverExpression =
                    Expression.Call(resolverInstance, method, parameters);

                Expression handleResult = HandleResult(
                    resolverExpression, method.ReturnType);

                return Expression.Lambda<SubscribeResolverDelegate>(
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
            if (resultType == typeof(Task<IAsyncEnumerable<object>>))
            {
                return resolverExpression;
            }

            if (typeof(Task).IsAssignableFrom(resultType)
                && resultType.IsGenericType)
            {
                Type subscriptionType = resultType.GetGenericArguments().First();

                if (subscriptionType.IsGenericType)
                {
                    Type typeDefinition = subscriptionType.GetGenericTypeDefinition();
                    if (typeDefinition == typeof(IAsyncEnumerable<>))
                    {
                        return AwaitAsyncEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IEnumerable<>))
                    {
                        return AwaitEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IQueryable<>))
                    {
                        return AwaitQueryable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IObservable<>))
                    {
                        return AwaitObservable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                }
            }

            if (resultType.IsGenericType)
            {
                Type typeDefinition = resultType.GetGenericTypeDefinition();

                if (typeDefinition == typeof(IAsyncEnumerable<>))
                {
                    return WrapAsyncEnumerable(
                        resolverExpression,
                        resultType.GetGenericArguments().Single());
                }
                else if (typeDefinition == typeof(IEnumerable<>))
                {
                    return WrapEnumerable(
                        resolverExpression,
                        resultType.GetGenericArguments().Single());
                }
                else if (typeDefinition == typeof(IQueryable<>))
                {
                    return WrapQueryable(
                        resolverExpression,
                        resultType.GetGenericArguments().Single());
                }
                else if (typeDefinition == typeof(IObservable<>))
                {
                    return WrapObservable(
                        resolverExpression,
                        resultType.GetGenericArguments().Single());
                }
            }

            throw new NotSupportedException(
                "The specified return type is not supported for a " +
                $"subscribe method `{resultType.FullName}`.");
        }

        private static MethodCallExpression AwaitAsyncEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitAsyncEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitQueryable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitObservable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitObservable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression WrapAsyncEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapAsyncEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(wrapResultHelper, taskExpression);
        }

        private static MethodCallExpression WrapEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(wrapResultHelper, taskExpression);
        }

        private static MethodCallExpression WrapQueryable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapQueryable.MakeGenericMethod(valueType);
            return Expression.Call(wrapResultHelper, taskExpression);
        }

        private static MethodCallExpression WrapObservable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapObservable.MakeGenericMethod(valueType);
            return Expression.Call(wrapResultHelper, taskExpression);
        }
    }
}
