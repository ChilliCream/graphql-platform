using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Properties;
using HotChocolate.Utilities;
using static System.Linq.Expressions.Expression;
using static HotChocolate.Resolvers.CodeGeneration.ArgumentHelper;

#nullable enable

namespace HotChocolate.Resolvers.Expressions
{
    internal sealed class ResolveCompiler : ResolverCompiler
    {
        private static readonly MethodInfo _awaitTaskHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitTaskHelper))!;
        private static readonly MethodInfo _awaitValueTaskHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitValueTaskHelper))!;
        private static readonly MethodInfo _wrapResultHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.WrapResultHelper))!;

        public FieldResolver Compile(ResolverDescriptor descriptor)
        {
            if (descriptor.Field.Member is not null)
            {
                FieldResolverDelegate resolver;
                PureFieldDelegate? pureResolver = null;

                if (descriptor.Field.Member is MethodInfo { IsStatic: true })
                {
                    resolver = CreateStaticResolver(descriptor.Field.Member, descriptor.SourceType);
                }
                else
                {
                    resolver = CreateResolver(
                        descriptor.Field.Member,
                        descriptor.SourceType,
                        descriptor.ResolverType ?? descriptor.SourceType);

                    pureResolver = CreatePureResolver(
                        descriptor.Field.Member,
                        descriptor.SourceType,
                        descriptor.ResolverType ?? descriptor.SourceType);
                }

                return new FieldResolver(
                    descriptor.Field.TypeName,
                    descriptor.Field.FieldName,
                    resolver,
                    pureResolver);
            }

            if (descriptor.Field.Expression is LambdaExpression lambda)
            {
                Expression resolver = CreateResolverClassInstance(
                    Context,
                    descriptor.SourceType,
                    descriptor.ResolverType ?? descriptor.SourceType);

                Expression<FieldResolverDelegate> resolve =
                    Lambda<FieldResolverDelegate>(
                        HandleResult(
                            Invoke(lambda, resolver),
                            lambda.ReturnType),
                        Context);

                return new FieldResolver(
                    descriptor.Field.TypeName,
                    descriptor.Field.FieldName,
                    resolve.Compile());
            }

            throw new NotSupportedException();
        }

        public FieldResolverDelegates Compile(
            Type sourceType,
            MemberInfo member,
            Type? resolverType = null)
        {
            if (sourceType is null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            FieldResolverDelegate resolver;
            PureFieldDelegate? pureResolver = null;

            if (member is MethodInfo { IsStatic: true })
            {
                resolver = CreateStaticResolver(member, sourceType);
            }
            else
            {
                resolver = CreateResolver(member, sourceType, resolverType ?? sourceType);
                pureResolver = CreatePureResolver(member, sourceType, resolverType ?? sourceType);
            }

            return new(resolver, pureResolver);
        }

        public FieldResolverDelegates Compile<TResolver>(
            Expression<Func<TResolver, object?>> propertyOrMethod,
            Type? sourceType = null)
        {
            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            MemberInfo member = propertyOrMethod.TryExtractMember();

            if (member is PropertyInfo or MethodInfo)
            {
                Type source = sourceType ?? typeof(TResolver);
                Type? resolver = sourceType is null ? typeof(TResolver) : null;
                return Compile(source, member, resolver);
            }

            throw new ArgumentException(
                TypeResources.ObjectTypeDescriptor_MustBePropertyOrMethod,
                nameof(member));
        }

        private FieldResolverDelegate CreateResolver(
            MemberInfo member,
            Type sourceType,
            Type resolverType)
        {
            if (member is MethodInfo method)
            {
                Expression resolver =
                    CreateResolverClassInstance(Context, sourceType, resolverType);
                Expression[] parameters =
                    CreateParameters(Context, method.GetParameters(), sourceType);
                MethodCallExpression resolverExpression = Call(resolver, method, parameters);
                Expression handleResult = HandleResult(resolverExpression, method.ReturnType);
                return Lambda<FieldResolverDelegate>(handleResult, Context).Compile();
            }

            if (member is PropertyInfo property)
            {
                Expression resolver =
                    CreateResolverClassInstance(Context, sourceType, resolverType);
                MemberExpression propertyAccessor = Property(resolver, property);
                Expression handleResult = HandleResult(propertyAccessor, property.PropertyType);
                return Lambda<FieldResolverDelegate>(handleResult, Context).Compile();
            }

            throw new NotSupportedException();
        }

        private FieldResolverDelegate CreateStaticResolver(MemberInfo member, Type sourceType)
        {
            if (member is MethodInfo method)
            {
                Expression[] parameters =
                    CreateParameters(Context, method.GetParameters(), sourceType);
                MethodCallExpression resolverExpression = Call(method, parameters);
                Expression handleResult = HandleResult(resolverExpression, method.ReturnType);
                return Lambda<FieldResolverDelegate>(handleResult, Context).Compile();
            }

            throw new NotSupportedException();
        }

        private PureFieldDelegate? CreatePureResolver(
            MemberInfo member,
            Type sourceType,
            Type resolverType)
        {
            if (member is PropertyInfo property && IsPureResult(property.PropertyType))
            {
                Expression resolver =
                    CreateResolverClassInstance(PureContext, sourceType, resolverType);
                MemberExpression propertyAccessor = Property(resolver, property);
                Expression result = Convert(propertyAccessor, typeof(object));
                return Lambda<PureFieldDelegate>(result, PureContext).Compile();
            }

            if (member is MethodInfo method &&
                IsPureMethod(method, resolverType) &&
                IsPureResult(method.ReturnType))
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (IsPure(parameters, sourceType))
                {
                    Expression resolver =
                        CreateResolverClassInstance(PureContext, sourceType, resolverType);
                    Expression[] parameterResolvers =
                        CreateParameters(PureContext, method.GetParameters(), sourceType);
                    MethodCallExpression resolverCall = Call(resolver, method, parameterResolvers);
                    Expression result = Convert(resolverCall, typeof(object));
                    return Lambda<PureFieldDelegate>(result, PureContext).Compile();
                }
            }

            return null;
        }

        // Create an expression to get the resolver class instance.
        private static Expression CreateResolverClassInstance(
            ParameterExpression context,
            Type sourceType,
            Type resolverType)
        {
            MethodInfo resolverMethod = sourceType == resolverType
                ? Parent.MakeGenericMethod(sourceType)
                : Resolver.MakeGenericMethod(resolverType);
            return Call(context, resolverMethod);
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

        private static bool IsPureMethod(
            MethodInfo methodInfo,
            Type resolverType)
        {
            if (methodInfo.IsDefined(typeof(PureAttribute)))
            {
                return true;
            }

            ConstructorInfo[] constructors = resolverType.GetConstructors();
            return constructors.Length == 1 && constructors[0].GetParameters().Length == 0;
        }

        private static bool IsPureResult(Type resultType)
        {
            if (resultType == typeof(ValueTask<object>))
            {
                return false;
            }

            if (typeof(IExecutable).IsAssignableFrom(resultType) ||
                typeof(IQueryable).IsAssignableFrom(resultType) ||
                typeof(Task).IsAssignableFrom(resultType))
            {
                return false;
            }

            if (resultType.IsGenericType)
            {
                Type type = resultType.GetGenericTypeDefinition();
                if (type == typeof(ValueTask<>) ||
                    type == typeof(IAsyncEnumerable<>))
                {
                    return false;
                }
            }

            return true;
        }

        private static MethodCallExpression AwaitTaskMethodCall(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskHelper.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskMethodCall(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskHelper.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression WrapResult(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapResultHelper.MakeGenericMethod(valueType);
            return Call(wrapResultHelper, taskExpression);
        }
    }
}
