using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HotChocolate.Internal;
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
    internal sealed class DefaultResolverCompiler : IResolverCompiler
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

        public DefaultResolverCompiler(
            IEnumerable<IParameterExpressionBuilder>? customParameterExpressionBuilders)
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

            if (customParameterExpressionBuilders is not null)
            {
                // then we will add custom parameter expression builder and
                // give the user a chance to override our implicit expression builder.
                list.AddRange(customParameterExpressionBuilders);
            }

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
            list.Add(new ClaimsPrincipalParameterExpressionBuilder());
            list.Add(new PathParameterExpressionBuilder());

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
                Expression[] parameterExpr = CreateParameters(_context, parameters);
                Expression subscribeResolver = Call(owner, method, parameterExpr);
                subscribeResolver = EnsureSubscribeResult(subscribeResolver, method.ReturnType);
                return Lambda<SubscribeResolverDelegate>(subscribeResolver, _context).Compile();
            }

            throw new ArgumentException(
                DefaultResolverCompilerService_CompileSubscribe_OnlyMethodsAllowed,
                nameof(member));
        }

        /// <inheritdoc />
        public IEnumerable<ParameterInfo> GetArgumentParameters(ParameterInfo[] parameters)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                IParameterExpressionBuilder builder =
                    GetParameterExpressionBuilder(parameter);

                if (builder.Kind == ArgumentKind.Argument)
                {
                    yield return parameter;
                }
            }
        }

        private FieldResolverDelegate CompileStaticResolver(MethodInfo method, Type source)
        {
            Expression[] parameters = CreateParameters(_context, method.GetParameters());
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
                Expression[] parameterExpr = CreateParameters(_context, parameters);
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
                    Expression[] parameterExpr = CreateParameters(_pureContext, parameters);
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
                    GetParameterExpressionBuilder(parameter);

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
            ParameterInfo[] parameters)
        {
            var parameterResolvers = new Expression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                IParameterExpressionBuilder builder =
                    GetParameterExpressionBuilder(parameter);

                parameterResolvers[i] = builder.Build(parameter, context);
            }

            return parameterResolvers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IParameterExpressionBuilder GetParameterExpressionBuilder(ParameterInfo parameter)
        {
            if (_cache.TryGetValue(parameter, out var cached))
            {
                return cached;
            }

            foreach (IParameterExpressionBuilder builder in _parameterExpressionBuilders)
            {
                if (builder.CanHandle(parameter))
                {
#if NETSTANDARD
                    _cache[parameter] = builder;
#else
                    _cache.TryAdd(parameter, builder);
#endif
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
}
