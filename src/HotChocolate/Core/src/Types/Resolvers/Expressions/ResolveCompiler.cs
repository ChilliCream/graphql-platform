using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers.Expressions.Parameters;

namespace HotChocolate.Resolvers.Expressions
{
    public sealed class ResolveCompiler : ResolverCompiler
    {
        private static readonly MethodInfo _awaitTaskHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitTaskHelper));
        private static readonly MethodInfo _awaitValueTaskHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitValueTaskHelper));
        private static readonly MethodInfo _wrapResultHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.WrapResultHelper));

        public ResolveCompiler()
        {
        }

        public ResolveCompiler(
            IEnumerable<IResolverParameterCompiler> compilers)
            : base(compilers)
        {
        }

        public FieldResolver Compile(ResolverDescriptor descriptor)
        {
            if (descriptor.Field.Member is not null)
            {
                FieldResolverDelegate resolver;

                if (descriptor.Field.Member is MethodInfo m && m.IsStatic)
                {
                    resolver = CreateResolver(
                        descriptor.Field.Member,
                        descriptor.SourceType);
                }
                else
                {
                    resolver = CreateResolver(
                        CreateResolverInstance(descriptor),
                        descriptor.Field.Member,
                        descriptor.SourceType);
                }

                return new FieldResolver(
                    descriptor.Field.TypeName,
                    descriptor.Field.FieldName,
                    resolver);
            }

            if (descriptor.Field.Expression is LambdaExpression lambda)
            {
                var resolver = Expression.Lambda<FieldResolverDelegate>(
                    HandleResult(
                        Expression.Invoke(lambda, CreateResolverInstance(descriptor)),
                        lambda.ReturnType),
                    Context);

                return new FieldResolver(
                    descriptor.Field.TypeName,
                    descriptor.Field.FieldName,
                    resolver.Compile());
            }

            throw new NotSupportedException();
        }

        private Expression CreateResolverInstance(ResolverDescriptor descriptor)
        {
            MethodInfo resolverMethod = descriptor.ResolverType is null
                ? Parent.MakeGenericMethod(descriptor.SourceType)
                : Resolver.MakeGenericMethod(descriptor.ResolverType);

            return Expression.Call(Context, resolverMethod);
        }

        private FieldResolverDelegate CreateResolver(
            MemberInfo member,
            Type sourceType)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member is MethodInfo method)
            {
                IEnumerable<Expression> parameters =
                    CreateParameters(method.GetParameters(), sourceType);
                MethodCallExpression resolverExpression = Expression.Call(method, parameters);
                Expression handleResult = HandleResult(resolverExpression, method.ReturnType);
                return Expression.Lambda<FieldResolverDelegate>(handleResult, Context).Compile();
            }

            throw new NotSupportedException();
        }

        private FieldResolverDelegate CreateResolver(
            Expression resolverInstance,
            MemberInfo member,
            Type sourceType)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member is MethodInfo method)
            {
                IEnumerable<Expression> parameters = CreateParameters(
                    method.GetParameters(), sourceType);
                MethodCallExpression resolverExpression =
                    Expression.Call(resolverInstance, method, parameters);
                Expression handleResult = HandleResult(resolverExpression, method.ReturnType);
                return Expression.Lambda<FieldResolverDelegate>(handleResult, Context).Compile();
            }

            if (member is PropertyInfo property)
            {
                MemberExpression propertyAccessor = Expression.Property(resolverInstance, property);
                Expression handleResult = HandleResult(propertyAccessor, property.PropertyType);
                return Expression.Lambda<FieldResolverDelegate>(handleResult, Context).Compile();
            }

            throw new NotSupportedException();
        }

        private static Expression HandleResult(
            Expression resolverExpression,
            Type resultType)
        {
            if (resultType == typeof(ValueTask<object>))
            {
                return resolverExpression;
            }

            if (typeof(Task).IsAssignableFrom(resultType) &&
                resultType.IsGenericType)
            {
                return AwaitTaskMethodCall(
                    resolverExpression,
                    resultType.GetGenericArguments()[0]);
            }

            if (resultType.IsGenericType
                && resultType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                return AwaitValueTaskMethodCall(
                    resolverExpression,
                    resultType.GetGenericArguments()[0]);
            }

            return WrapResult(resolverExpression, resultType);
        }

        private static MethodCallExpression AwaitTaskMethodCall(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskHelper.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskMethodCall(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskHelper.MakeGenericMethod(valueType);
            return Expression.Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression WrapResult(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapResultHelper.MakeGenericMethod(valueType);
            return Expression.Call(wrapResultHelper, taskExpression);
        }
    }
}
