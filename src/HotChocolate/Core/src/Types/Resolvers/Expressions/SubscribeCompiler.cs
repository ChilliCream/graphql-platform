using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Execution;

#nullable enable

namespace HotChocolate.Resolvers.Expressions
{

    public sealed class SubscribeCompiler
        : ResolverCompiler
    {
        private static readonly MethodInfo _awaitTaskSourceStreamGeneric =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitTaskSourceStreamGeneric))!;
        private static readonly MethodInfo _awaitTaskSourceStream =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitTaskSourceStream))!;
        private static readonly MethodInfo _awaitTaskAsyncEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitTaskAsyncEnumerable))!;
        private static readonly MethodInfo _awaitTaskEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitTaskEnumerable))!;
        private static readonly MethodInfo _awaitTaskQueryable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitTaskQueryable))!;
        private static readonly MethodInfo _awaitTaskObservable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitTaskObservable))!;
        private static readonly MethodInfo _awaitValueTaskSourceStreamGeneric =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitValueTaskSourceStreamGeneric))!;
        private static readonly MethodInfo _awaitValueTaskAsyncEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitValueTaskAsyncEnumerable))!;
        private static readonly MethodInfo _awaitValueTaskEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitValueTaskEnumerable))!;
        private static readonly MethodInfo _awaitValueTaskQueryable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitValueTaskQueryable))!;
        private static readonly MethodInfo _awaitValueTaskObservable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.AwaitValueTaskObservable))!;
        private static readonly MethodInfo _wrapSourceStreamGeneric =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapSourceStreamGeneric))!;
        private static readonly MethodInfo _wrapSourceStream =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapSourceStream))!;
        private static readonly MethodInfo _wrapAsyncEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapAsyncEnumerable))!;
        private static readonly MethodInfo _wrapEnumerable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapEnumerable))!;
        private static readonly MethodInfo _wrapQueryable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapQueryable))!;
        private static readonly MethodInfo _wrapObservable =
            typeof(SubscribeExpressionHelper)
                .GetMethod(nameof(SubscribeExpressionHelper.WrapObservable))!;

        public SubscribeResolverDelegate Compile(
            Type sourceType,
            Type? resolverType,
            MemberInfo member)
        {
            MethodInfo resolverMethod = resolverType is null
                ? Parent.MakeGenericMethod(sourceType)
                : Resolver.MakeGenericMethod(resolverType);

            Expression resolverInstance = Expression.Call(
                Context, resolverMethod);

            return CreateResolver(
                resolverInstance,
                member,
                sourceType);
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
                    handleResult, Context).Compile();
            }

            throw new NotSupportedException();
        }

        private static Expression HandleResult(
            Expression resolverExpression,
            Type resultType)
        {
            if (resultType == typeof(ValueTask<ISourceStream>))
            {
                return resolverExpression;
            }

            if (typeof(Task).IsAssignableFrom(resultType)
                && resultType.IsGenericType)
            {
                Type subscriptionType = resultType.GetGenericArguments().First();

                if (subscriptionType == typeof(ISourceStream))
                {
                    return AwaitTaskSourceStream(resolverExpression);
                }
                else if (subscriptionType.IsGenericType)
                {
                    Type typeDefinition = subscriptionType.GetGenericTypeDefinition();
                    if (typeDefinition == typeof(ISourceStream<>))
                    {
                        return AwaitTaskSourceStreamGeneric(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IAsyncEnumerable<>))
                    {
                        return AwaitTaskAsyncEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IEnumerable<>))
                    {
                        return AwaitTaskEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IQueryable<>))
                    {
                        return AwaitTaskQueryable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IObservable<>))
                    {
                        return AwaitTaskObservable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                }
            }

            if (resultType.IsGenericType
                && resultType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                Type subscriptionType = resultType.GetGenericArguments().First();

                if (subscriptionType.IsGenericType)
                {
                    Type typeDefinition = subscriptionType.GetGenericTypeDefinition();
                    if (typeDefinition == typeof(ISourceStream<>))
                    {
                        return AwaitValueTaskSourceStreamGeneric(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IAsyncEnumerable<>))
                    {
                        return AwaitValueTaskAsyncEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IEnumerable<>))
                    {
                        return AwaitValueTaskEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IQueryable<>))
                    {
                        return AwaitValueTaskQueryable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                    else if (typeDefinition == typeof(IObservable<>))
                    {
                        return AwaitValueTaskObservable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }
                }
            }

            if (resultType == typeof(ISourceStream))
            {
                return WrapSourceStream(resolverExpression);
            }

            if (resultType.IsGenericType)
            {
                Type typeDefinition = resultType.GetGenericTypeDefinition();
                if (typeDefinition == typeof(ISourceStream<>))
                {
                    return WrapSourceStreamGeneric(
                        resolverExpression,
                        resultType.GetGenericArguments().Single());
                }
                else if (typeDefinition == typeof(IAsyncEnumerable<>))
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

        private static MethodCallExpression AwaitTaskSourceStream(
            Expression taskExpression)
        {
            return Expression.Call(_awaitTaskSourceStream, taskExpression);
        }

        private static MethodCallExpression AwaitTaskSourceStreamGeneric(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskSourceStreamGeneric.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitTaskAsyncEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskAsyncEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitTaskEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitTaskQueryable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskQueryable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitTaskObservable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskObservable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskSourceStreamGeneric(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskSourceStreamGeneric.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskAsyncEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskAsyncEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskEnumerable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskQueryable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskQueryable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskObservable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskObservable.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression WrapSourceStream(Expression taskExpression)
        {
            return Expression.Call(_wrapSourceStream, taskExpression);
        }

        private static MethodCallExpression WrapSourceStreamGeneric(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapSourceStreamGeneric.MakeGenericMethod(valueType);
            return Expression.Call(wrapResultHelper, taskExpression);
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
