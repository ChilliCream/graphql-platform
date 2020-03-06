using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers.Expressions.Parameters;

namespace HotChocolate.Resolvers.Expressions
{
    internal sealed class ResolveCompiler : ResolverCompiler
    {
        private static readonly MethodInfo _awaitHelper =
            typeof(ExpressionHelper).GetMethod("AwaitHelper");
        private static readonly MethodInfo _wrapResultHelper =
            typeof(ExpressionHelper).GetMethod("WrapResultHelper");

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
            MethodInfo resolverMethod = descriptor.ResolverType is null
                ? Parent.MakeGenericMethod(descriptor.SourceType)
                : Resolver.MakeGenericMethod(descriptor.ResolverType);

            Expression resolverInstance = Expression.Call(
                Context, resolverMethod);

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
                    handleResult, Context).Compile();
            }

            if (member is PropertyInfo property)
            {
                MemberExpression propertyAccessor = Expression.Property(
                    resolverInstance, property);

                Expression handleResult = HandleResult(
                    propertyAccessor, property.PropertyType);

                return Expression.Lambda<FieldResolverDelegate>(
                    handleResult, Context).Compile();
            }

            throw new NotSupportedException();
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
}
