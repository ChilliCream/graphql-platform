using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Utilities;
using static System.Linq.Expressions.Expression;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Resolvers.ResolveResultHelper;
using static HotChocolate.Resolvers.SubscribeResultHelper;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// This class provides some helper methods to compile resolvers for dynamic schemas.
    /// </summary>
    internal sealed class DefaultResolverCompilerService : IResolverCompilerService
    {
        private static readonly ParameterExpression _context =
            Parameter(typeof(IResolverContext), "context");
        private static readonly ParameterExpression _pureContext =
            Parameter(typeof(IPureResolverContext), "context");
        private static readonly MethodInfo _parent =
            typeof(IPureResolverContext).GetMethod(nameof(IPureResolverContext.Parent))!;
        private static readonly MethodInfo _resolver =
            typeof(IPureResolverContext).GetMethod(nameof(IPureResolverContext.Resolver))!;

        private readonly Dictionary<ParameterInfo, IParameterExpressionBuilder> _cache = new();
        private readonly List<IParameterExpressionBuilder> _parameterExpressionBuilders;

        public DefaultResolverCompilerService(IEnumerable<IParameterExpressionBuilder> customParameterExpressionBuilders)
        {
            // explicit internal expression builders will be added first.
            var list = new List<IParameterExpressionBuilder>
            {
                new ParentParameterExpressionBuilder(),
                new ServiceParameterExpressionBuilder(),
                new ArgumentParameterExpressionBuilder(),
                new GlobalStateParameterExpressionBuilder(),
                new ScopedStateParameterExpressionBuilder(),
                new LocalStateParameterExpressionBuilder(),
                new EventMessageParameterExpressionBuilder(),
                new ScopedServiceParameterExpressionBuilder(),
            };

            // then we will add custom parameter expression builder and
            // give the user a chance to override our implicit expression builder.
            list.AddRange(customParameterExpressionBuilders);

            // then we add the internal implicit expression builder.
            list.Add(new DocumentParameterExpressionBuilder());
            list.Add(new CancellationTokenParameterExpressionBuilder());
            list.Add(new ResolverContextParameterExpressionBuilder());
            list.Add(new PureResolverContextParameterExpressionBuilder());
            list.Add(new SchemaParameterExpressionBuilder());
            list.Add(new SelectionParameterExpressionBuilder());
            list.Add(new FieldSyntaxParameterExpressionBuilder());
            list.Add(new ObjectTypeParameterExpressionBuilder());
            list.Add(new OperationParameterExpressionBuilder());
            list.Add(new FieldParameterExpressionBuilder());

            // last we qdd the implicit argument expression builder which represents our default
            // expression builder and will compile all that is left to GraphQL field arguments.
            list.Add(new ImplicitArgumentParameterExpressionBuilder());

            _parameterExpressionBuilders = list;
        }

        /// <inheritdoc />
        public FieldResolverDelegates CompileResolve<TResolver>(
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
                return CompileResolve(member, source, resolver);
            }

            throw new ArgumentException(
                ObjectTypeDescriptor_MustBePropertyOrMethod,
                nameof(member));
        }

        /// <inheritdoc />
        public FieldResolverDelegates CompileResolve(
            LambdaExpression lambda,
            Type sourceType,
            Type? resolverType = null)
        {
            resolverType ??= sourceType;

            Expression owner = CreateResolverOwner(_context, sourceType, resolverType);
            Expression resolver = Invoke(lambda, owner);
            resolver = EnsureResolveResult(resolver, lambda.ReturnType);
            return new(Lambda<FieldResolverDelegate>(resolver, _context).Compile());
        }

        /// <inheritdoc />
        public FieldResolverDelegates CompileResolve(
            MemberInfo member,
            Type? sourceType = null,
            Type? resolverType = null)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            FieldResolverDelegate resolver;
            PureFieldDelegate? pureResolver = null;

            sourceType ??= member.ReflectedType ?? member.DeclaringType!;
            resolverType ??= sourceType;

            if (member is MethodInfo { IsStatic: true } method)
            {
                resolver = CompileStaticResolver(method, sourceType);
            }
            else
            {
                resolver = CreateResolver(member, sourceType, resolverType);
                pureResolver = TryCompilePureResolver(member, sourceType, resolverType);
            }

            return new(resolver, pureResolver);
        }

        /// <inheritdoc />
        public SubscribeResolverDelegate CompileSubscribe(
            MemberInfo member,
            Type? sourceType = null,
            Type? resolverType = null)
        {
            sourceType ??= member.ReflectedType ?? member.DeclaringType!;
            resolverType ??= sourceType;

            if (member is MethodInfo method)
            {
                ParameterInfo[] parameters = method.GetParameters();
                Expression owner = CreateResolverOwner(_context, sourceType, resolverType);
                Expression[] parameterExpr = CreateParameters(_context, parameters, sourceType);
                Expression subscribeResolver = Call(owner, method, parameterExpr);
                subscribeResolver = EnsureSubscribeResult(subscribeResolver, method.ReturnType);
                return Lambda<SubscribeResolverDelegate>(subscribeResolver, _context).Compile();
            }

            throw new ArgumentException(
                DefaultResolverCompilerService_CompileSubscribe_OnlyMethodsAllowed,
                nameof(member));
        }

        /// <inheritdoc />
        public IEnumerable<ParameterInfo> GetArgumentParameters(
            ParameterInfo[] parameters,
            Type sourceType)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                IParameterExpressionBuilder builder =
                    GetParameterExpressionBuilder(parameter, sourceType);

                if (builder.Kind == ArgumentKind.Argument)
                {
                    yield return parameter;
                }
            }
        }

        private FieldResolverDelegate CompileStaticResolver(MethodInfo method, Type source)
        {
            Expression[] parameters = CreateParameters(_context, method.GetParameters(), source);
            Expression resolver = Call(method, parameters);
            resolver = EnsureResolveResult(resolver, method.ReturnType);
            return Lambda<FieldResolverDelegate>(resolver, _context).Compile();
        }

        private FieldResolverDelegate CreateResolver(
            MemberInfo member,
            Type source,
            Type resolverType)
        {
            if (member is PropertyInfo property)
            {
                Expression owner = CreateResolverOwner(_context, source, resolverType);
                Expression propResolver = Property(owner, property);
                propResolver = EnsureResolveResult(propResolver, property.PropertyType);
                return Lambda<FieldResolverDelegate>(propResolver, _context).Compile();
            }

            if (member is MethodInfo method)
            {
                ParameterInfo[] parameters = method.GetParameters();
                Expression owner = CreateResolverOwner(_context, source, resolverType);
                Expression[] parameterExpr = CreateParameters(_context, parameters, source);
                Expression methodResolver = Call(owner, method, parameterExpr);
                methodResolver = EnsureResolveResult(methodResolver, method.ReturnType);
                return Lambda<FieldResolverDelegate>(methodResolver, _context).Compile();
            }

            throw new NotSupportedException(
                DefaultResolverCompilerService_CreateResolver_ArgumentValudationError);
        }

        private PureFieldDelegate? TryCompilePureResolver(
            MemberInfo member,
            Type source,
            Type resolver)
        {
            if (member is PropertyInfo property && IsPureResolverResult(property.PropertyType))
            {
                Expression owner = CreateResolverOwner(_pureContext, source, resolver);
                Expression propertyResolver = Property(owner, property);

                if (property.PropertyType != typeof(object))
                {
                    propertyResolver = Convert(propertyResolver, typeof(object));
                }

                return Lambda<PureFieldDelegate>(propertyResolver, _pureContext).Compile();
            }

            if (member is MethodInfo method)
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (IsPureResolver(method, parameters, source))
                {
                    Expression owner = CreateResolverOwner(_pureContext, source, resolver);
                    Expression[] parameterExpr = CreateParameters(_pureContext, parameters, source);
                    Expression methodResolver = Call(owner, method, parameterExpr);

                    if (method.ReturnType != typeof(object))
                    {
                        methodResolver = Convert(methodResolver, typeof(object));
                    }

                    return Lambda<PureFieldDelegate>(methodResolver, _pureContext).Compile();
                }
            }

            return null;
        }

        private bool IsPureResolver(MethodInfo method, ParameterInfo[] parameters, Type source)
        {
            if (!IsPureResolverResult(method.ReturnType))
            {
                return false;
            }

            foreach (ParameterInfo parameter in parameters)
            {
                IParameterExpressionBuilder builder =
                    GetParameterExpressionBuilder(parameter, source);

                if (!builder.IsPure)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPureResolverResult(Type resultType)
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

        // Create an expression to get the resolver class instance.
        private static Expression CreateResolverOwner(
            ParameterExpression context,
            Type source,
            Type resolver)
        {
            MethodInfo resolverMethod = source == resolver
                ? _parent.MakeGenericMethod(source)
                : _resolver.MakeGenericMethod(resolver);
            return Call(context, resolverMethod);
        }

        private Expression[] CreateParameters(
            ParameterExpression context,
            ParameterInfo[] parameters,
            Type source)
        {
            var parameterResolvers = new Expression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                IParameterExpressionBuilder builder =
                    GetParameterExpressionBuilder(parameter, source);

                parameterResolvers[i] = builder.Build(parameter, source, context);
            }

            return parameterResolvers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IParameterExpressionBuilder GetParameterExpressionBuilder(
            ParameterInfo parameter,
            Type source)
        {
            if (_cache.TryGetValue(parameter, out var cached))
            {
                return cached;
            }

            foreach (IParameterExpressionBuilder builder in _parameterExpressionBuilders)
            {
                if(builder.CanHandle(parameter, source))
                {
                    _cache.TryAdd(parameter, builder);
                    return builder;
                }
            }

            throw new NotSupportedException(DefaultResolverCompilerService_Misconfigured);
        }

        public void Dispose()
        {
            _cache.Clear();
        }
    }

    internal static class ResolveResultHelper
    {
        private static readonly MethodInfo _awaitTaskHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitTaskHelper))!;
        private static readonly MethodInfo _awaitValueTaskHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitValueTaskHelper))!;
        private static readonly MethodInfo _wrapResultHelper =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.WrapResultHelper))!;

        public static Expression EnsureResolveResult(Expression resolver, Type result)
        {
            if (result == typeof(ValueTask<object>))
            {
                return resolver;
            }

            if (typeof(Task).IsAssignableFrom(result) &&
                result.IsGenericType)
            {
                return AwaitTaskMethodCall(
                    resolver,
                    result.GetGenericArguments()[0]);
            }

            if (result.IsGenericType
                && result.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                return AwaitValueTaskMethodCall(
                    resolver,
                    result.GetGenericArguments()[0]);
            }

            return WrapResult(resolver, result);
        }

        private static MethodCallExpression AwaitTaskMethodCall(
            Expression taskExpression, Type value)
        {
            MethodInfo awaitHelper = _awaitTaskHelper.MakeGenericMethod(value);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskMethodCall(
            Expression taskExpression, Type value)
        {
            MethodInfo awaitHelper = _awaitValueTaskHelper.MakeGenericMethod(value);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression WrapResult(
            Expression taskExpression, Type value)
        {
            MethodInfo wrapResultHelper = _wrapResultHelper.MakeGenericMethod(value);
            return Call(wrapResultHelper, taskExpression);
        }
    }

    internal static class SubscribeResultHelper
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

        public static Expression EnsureSubscribeResult(
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

                if (subscriptionType.IsGenericType)
                {
                    Type typeDefinition = subscriptionType.GetGenericTypeDefinition();
                    if (typeDefinition == typeof(ISourceStream<>))
                    {
                        return AwaitTaskSourceStreamGeneric(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }

                    if (typeDefinition == typeof(IAsyncEnumerable<>))
                    {
                        return AwaitTaskAsyncEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }

                    if (typeDefinition == typeof(IEnumerable<>))
                    {
                        return AwaitTaskEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }

                    if (typeDefinition == typeof(IQueryable<>))
                    {
                        return AwaitTaskQueryable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }

                    if (typeDefinition == typeof(IObservable<>))
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

                    if (typeDefinition == typeof(IAsyncEnumerable<>))
                    {
                        return AwaitValueTaskAsyncEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }

                    if (typeDefinition == typeof(IEnumerable<>))
                    {
                        return AwaitValueTaskEnumerable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }

                    if (typeDefinition == typeof(IQueryable<>))
                    {
                        return AwaitValueTaskQueryable(
                            resolverExpression,
                            subscriptionType.GetGenericArguments().Single());
                    }

                    if (typeDefinition == typeof(IObservable<>))
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

                if (typeDefinition == typeof(IAsyncEnumerable<>))
                {
                    return WrapAsyncEnumerable(
                        resolverExpression,
                        resultType.GetGenericArguments().Single());
                }

                if (typeDefinition == typeof(IEnumerable<>))
                {
                    return WrapEnumerable(
                        resolverExpression,
                        resultType.GetGenericArguments().Single());
                }

                if (typeDefinition == typeof(IQueryable<>))
                {
                    return WrapQueryable(
                        resolverExpression,
                        resultType.GetGenericArguments().Single());
                }

                if (typeDefinition == typeof(IObservable<>))
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
            return Call(_awaitTaskSourceStream, taskExpression);
        }

        private static MethodCallExpression AwaitTaskSourceStreamGeneric(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskSourceStreamGeneric.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitTaskAsyncEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskAsyncEnumerable.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitTaskEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskEnumerable.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitTaskQueryable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskQueryable.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitTaskObservable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitTaskObservable.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskSourceStreamGeneric(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper =
                _awaitValueTaskSourceStreamGeneric.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskAsyncEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskAsyncEnumerable.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskEnumerable.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskQueryable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskQueryable.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression AwaitValueTaskObservable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo awaitHelper = _awaitValueTaskObservable.MakeGenericMethod(valueType);
            return Call(awaitHelper, taskExpression);
        }

        private static MethodCallExpression WrapSourceStream(Expression taskExpression)
        {
            return Call(_wrapSourceStream, taskExpression);
        }

        private static MethodCallExpression WrapSourceStreamGeneric(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapSourceStreamGeneric.MakeGenericMethod(valueType);
            return Call(wrapResultHelper, taskExpression);
        }

        private static MethodCallExpression WrapAsyncEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapAsyncEnumerable.MakeGenericMethod(valueType);
            return Call(wrapResultHelper, taskExpression);
        }

        private static MethodCallExpression WrapEnumerable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapEnumerable.MakeGenericMethod(valueType);
            return Call(wrapResultHelper, taskExpression);
        }

        private static MethodCallExpression WrapQueryable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapQueryable.MakeGenericMethod(valueType);
            return Call(wrapResultHelper, taskExpression);
        }

        private static MethodCallExpression WrapObservable(
            Expression taskExpression, Type valueType)
        {
            MethodInfo wrapResultHelper = _wrapObservable.MakeGenericMethod(valueType);
            return Call(wrapResultHelper, taskExpression);
        }
    }
}
