using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static System.Linq.Expressions.Expression;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Resolvers.ResolveResultHelper;
using static HotChocolate.Resolvers.SubscribeResultHelper;

#nullable enable

namespace HotChocolate.Resolvers;

/// <summary>
/// This class provides some helper methods to compile resolvers for dynamic schemas.
/// </summary>
internal sealed class DefaultResolverCompiler : IResolverCompiler
{
    private static readonly IReadOnlyList<IParameterExpressionBuilder> _empty = [];

    private static readonly ParameterExpression _context =
        Parameter(typeof(IResolverContext), "context");

    private static readonly ParameterExpression _pureContext =
        Parameter(typeof(IResolverContext), "context");

    private static readonly MethodInfo _parent =
        typeof(IResolverContext).GetMethod(nameof(IResolverContext.Parent))!;

    private static readonly MethodInfo _resolver =
        typeof(IResolverContext).GetMethod(nameof(IResolverContext.Resolver))!;

    private readonly Dictionary<ParameterInfo, IParameterExpressionBuilder> _cache = new();
    private readonly List<IParameterExpressionBuilder> _parameterExpressionBuilders;
    private readonly List<IParameterExpressionBuilder> _defaultParameterExpressionBuilders;
    private readonly List<IParameterFieldConfiguration> _parameterFieldConfigurations;
    private readonly ImplicitArgumentParameterExpressionBuilder _defaultExprBuilder = new();

    private readonly IReadOnlyDictionary<ParameterInfo, string> _emptyLookup =
        new Dictionary<ParameterInfo, string>();

    public DefaultResolverCompiler(
        IServiceProvider schemaServiceProvider,
        IEnumerable<IParameterExpressionBuilder>? customParameterExpressionBuilders)
    {
        var appServiceProvider = schemaServiceProvider.GetService<IApplicationServiceProvider>();
        var serviceInspector = appServiceProvider?.GetService<IServiceProviderIsService>();

        var custom = customParameterExpressionBuilders is not null
            ? [..customParameterExpressionBuilders,]
            : new List<IParameterExpressionBuilder>();

        // explicit internal expression builders will be added first.
        var expressionBuilders = new List<IParameterExpressionBuilder>
        {
            new ParentParameterExpressionBuilder(),
            new ServiceParameterExpressionBuilder(),
            new ArgumentParameterExpressionBuilder(),
            new GlobalStateParameterExpressionBuilder(),
            new ScopedStateParameterExpressionBuilder(),
            new LocalStateParameterExpressionBuilder(),
            new IsSelectedParameterExpressionBuilder(),
            new EventMessageParameterExpressionBuilder(),
        };

        if (customParameterExpressionBuilders is not null)
        {
            // then we will add custom parameter expression builder and
            // give the user a chance to override our implicit expression builder.
            foreach (var builder in custom)
            {
                if (!builder.IsDefaultHandler)
                {
                    expressionBuilders.Add(builder);
                }
            }
        }

        // then we add the internal implicit expression builder.
        expressionBuilders.Add(new DocumentParameterExpressionBuilder());
        expressionBuilders.Add(new CancellationTokenParameterExpressionBuilder());
        expressionBuilders.Add(new ResolverContextParameterExpressionBuilder());
        expressionBuilders.Add(new SchemaParameterExpressionBuilder());
        expressionBuilders.Add(new SelectionParameterExpressionBuilder());
        expressionBuilders.Add(new FieldSyntaxParameterExpressionBuilder());
        expressionBuilders.Add(new ObjectTypeParameterExpressionBuilder());
        expressionBuilders.Add(new OperationDefinitionParameterExpressionBuilder());
        expressionBuilders.Add(new OperationParameterExpressionBuilder());
        expressionBuilders.Add(new FieldParameterExpressionBuilder());
        expressionBuilders.Add(new ClaimsPrincipalParameterExpressionBuilder());
        expressionBuilders.Add(new PathParameterExpressionBuilder());

        if (serviceInspector is not null)
        {
            expressionBuilders.Add(new InferredServiceParameterExpressionBuilder(serviceInspector));
        }

        if (customParameterExpressionBuilders is not null)
        {
            var defaultParameterExpressionBuilders = new List<IParameterExpressionBuilder>();

            // last we will add all custom default handlers. This will give these handlers a chance
            // to apply logic only on arguments.
            foreach (var builder in custom)
            {
                if (builder.IsDefaultHandler)
                {
                    defaultParameterExpressionBuilders.Add(builder);
                }
            }

            _defaultParameterExpressionBuilders = defaultParameterExpressionBuilders;
        }
        else
        {
            _defaultParameterExpressionBuilders = [];
        }

        _parameterExpressionBuilders = expressionBuilders;

        var parameterFieldConfigurations = new List<IParameterFieldConfiguration>();

        foreach (var builder in _parameterExpressionBuilders)
        {
            if (builder is IParameterFieldConfiguration configuration)
            {
                parameterFieldConfigurations.Add(configuration);
            }
        }

        foreach (var builder in _defaultParameterExpressionBuilders)
        {
            if (builder is IParameterFieldConfiguration configuration)
            {
                parameterFieldConfigurations.Add(configuration);
            }
        }

        _parameterFieldConfigurations = parameterFieldConfigurations;
    }

    /// <inheritdoc />
    public FieldResolverDelegates CompileResolve<TResolver>(
        Expression<Func<TResolver, object?>> propertyOrMethod,
        Type? sourceType = null,
        IReadOnlyDictionary<ParameterInfo, string>? argumentNames = null,
        IReadOnlyList<IParameterExpressionBuilder>? parameterExpressionBuilders = null)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        var member = propertyOrMethod.TryExtractMember();

        if (member is PropertyInfo or MethodInfo)
        {
            var source = sourceType ?? typeof(TResolver);
            var resolver = sourceType is null
                ? typeof(TResolver)
                : null;
            return CompileResolve(
                member,
                source,
                resolver,
                argumentNames,
                parameterExpressionBuilders);
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
        if (lambda is null)
        {
            throw new ArgumentNullException(nameof(lambda));
        }

        resolverType ??= sourceType ?? throw new ArgumentNullException(nameof(sourceType));

        var owner = CreateResolverOwner(_context, sourceType, resolverType);
        Expression resolver = Invoke(lambda, owner);
        resolver = EnsureResolveResult(resolver, lambda.ReturnType);
        return new(Lambda<FieldResolverDelegate>(resolver, _context).Compile());
    }

    /// <inheritdoc />
    public FieldResolverDelegates CompileResolve(
        MemberInfo member,
        Type? sourceType = null,
        Type? resolverType = null,
        IReadOnlyDictionary<ParameterInfo, string>? argumentNames = null,
        IReadOnlyList<IParameterExpressionBuilder>? parameterExpressionBuilders = null)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        FieldResolverDelegate resolver;
        PureFieldDelegate? pureResolver = null;
        argumentNames ??= _emptyLookup;
        parameterExpressionBuilders ??= _empty;

        sourceType ??= member.ReflectedType ?? member.DeclaringType!;
        resolverType ??= sourceType;

        if (member is MethodInfo { IsStatic: true, } method)
        {
            resolver = CompileStaticResolver(method, argumentNames, parameterExpressionBuilders);
        }
        else if (member is PropertyInfo { GetMethod: { IsStatic: true, } getMethod, })
        {
            resolver = CompileStaticResolver(getMethod, argumentNames, parameterExpressionBuilders);
        }
        else
        {
            resolver = CreateResolver(
                member,
                sourceType,
                resolverType,
                argumentNames,
                parameterExpressionBuilders);

            pureResolver = TryCompilePureResolver(
                member,
                sourceType,
                resolverType,
                argumentNames,
                parameterExpressionBuilders);
        }

        return new(resolver, pureResolver);
    }

    /// <inheritdoc />
    public SubscribeResolverDelegate CompileSubscribe(
        MemberInfo member,
        Type? sourceType = null,
        Type? resolverType = null,
        IReadOnlyDictionary<ParameterInfo, string>? argumentNames = null,
        IReadOnlyList<IParameterExpressionBuilder>? parameterExpressionBuilders = null)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        sourceType ??= member.ReflectedType ?? member.DeclaringType!;
        resolverType ??= sourceType;
        argumentNames ??= _emptyLookup;
        parameterExpressionBuilders ??= _empty;

        if (member is MethodInfo method)
        {
            if (method.IsStatic)
            {
                var parameterExpr = CreateParameters(
                    _context,
                    method.GetParameters(),
                    argumentNames,
                    parameterExpressionBuilders);
                Expression subscribeResolver = Call(method, parameterExpr);
                subscribeResolver = EnsureSubscribeResult(subscribeResolver, method.ReturnType);
                return Lambda<SubscribeResolverDelegate>(subscribeResolver, _context).Compile();
            }
            else
            {
                var parameters = method.GetParameters();
                var owner = CreateResolverOwner(_context, sourceType, resolverType);
                var parameterExpr = CreateParameters(
                    _context,
                    parameters,
                    argumentNames,
                    parameterExpressionBuilders);
                Expression subscribeResolver = Call(owner, method, parameterExpr);
                subscribeResolver = EnsureSubscribeResult(subscribeResolver, method.ReturnType);
                return Lambda<SubscribeResolverDelegate>(subscribeResolver, _context).Compile();
            }
        }

        throw new ArgumentException(
            DefaultResolverCompilerService_CompileSubscribe_OnlyMethodsAllowed,
            nameof(member));
    }

    /// <inheritdoc />
    public IEnumerable<ParameterInfo> GetArgumentParameters(
        ParameterInfo[] parameters,
        IReadOnlyList<IParameterExpressionBuilder>? parameterExpressionBuilders = null)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        foreach (var parameter in parameters)
        {
            var builder =
                GetParameterExpressionBuilder(
                    parameter,
                    parameterExpressionBuilders ?? _empty);

            if (builder.Kind == ArgumentKind.Argument)
            {
                yield return parameter;
            }
        }
    }

    /// <inheritdoc />
    public void ApplyConfiguration(
        ParameterInfo[] parameters,
        ObjectFieldDescriptor descriptor)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        foreach (var parameter in parameters)
        {
            foreach (var configuration in _parameterFieldConfigurations)
            {
                if (configuration.CanHandle(parameter))
                {
                    configuration.ApplyConfiguration(parameter, descriptor);
                    break;
                }
            }
        }
    }

    private FieldResolverDelegate CompileStaticResolver(
        MethodInfo method,
        IReadOnlyDictionary<ParameterInfo, string> argumentNames,
        IReadOnlyList<IParameterExpressionBuilder> fieldParameterExpressionBuilders)
    {
        var parameters = CreateParameters(
            _context,
            method.GetParameters(),
            argumentNames,
            fieldParameterExpressionBuilders);
        Expression resolver = Call(method, parameters);
        resolver = EnsureResolveResult(resolver, method.ReturnType);
        return Lambda<FieldResolverDelegate>(resolver, _context).Compile();
    }

    private FieldResolverDelegate CreateResolver(
        MemberInfo member,
        Type source,
        Type resolverType,
        IReadOnlyDictionary<ParameterInfo, string> argumentNames,
        IReadOnlyList<IParameterExpressionBuilder> fieldParameterExpressionBuilders)
    {
        if (member is PropertyInfo property)
        {
            var owner = CreateResolverOwner(_context, source, resolverType);
            Expression propResolver = Property(owner, property);
            propResolver = EnsureResolveResult(propResolver, property.PropertyType);
            return Lambda<FieldResolverDelegate>(propResolver, _context).Compile();
        }

        if (member is MethodInfo method)
        {
            var parameters = method.GetParameters();
            var owner = CreateResolverOwner(_context, source, resolverType);
            var parameterExpr = CreateParameters(
                _context,
                parameters,
                argumentNames,
                fieldParameterExpressionBuilders);
            Expression methodResolver = Call(owner, method, parameterExpr);
            methodResolver = EnsureResolveResult(methodResolver, method.ReturnType);
            return Lambda<FieldResolverDelegate>(methodResolver, _context).Compile();
        }

        throw new NotSupportedException(
            DefaultResolverCompilerService_CreateResolver_ArgumentValidationError);
    }

    private PureFieldDelegate? TryCompilePureResolver(
        MemberInfo member,
        Type source,
        Type resolver,
        IReadOnlyDictionary<ParameterInfo, string> argumentNames,
        IReadOnlyList<IParameterExpressionBuilder> fieldParameterExpressionBuilders)
    {
        if (member is PropertyInfo property && IsPureResolverResult(property.PropertyType))
        {
            var owner = CreateResolverOwner(_pureContext, source, resolver);
            Expression propertyResolver = Property(owner, property);

            if (property.PropertyType != typeof(object))
            {
                propertyResolver = Convert(propertyResolver, typeof(object));
            }

            return Lambda<PureFieldDelegate>(propertyResolver, _pureContext).Compile();
        }

        if (member is MethodInfo method)
        {
            var parameters = method.GetParameters();

            if (IsPureResolver(method, parameters, fieldParameterExpressionBuilders))
            {
                var owner = CreateResolverOwner(_pureContext, source, resolver);
                var parameterExpr = CreateParameters(
                    _pureContext,
                    parameters,
                    argumentNames,
                    fieldParameterExpressionBuilders);
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

    private bool IsPureResolver(
        MethodInfo method,
        ParameterInfo[] parameters,
        IReadOnlyList<IParameterExpressionBuilder> fieldParameterExpressionBuilders)
    {
        if (!IsPureResolverResult(method.ReturnType))
        {
            return false;
        }

        foreach (var parameter in parameters)
        {
            var builder =
                GetParameterExpressionBuilder(parameter, fieldParameterExpressionBuilders);

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
            var type = resultType.GetGenericTypeDefinition();

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
        var resolverMethod = source == resolver
            ? _parent.MakeGenericMethod(source)
            : _resolver.MakeGenericMethod(resolver);
        return Call(context, resolverMethod);
    }

    private Expression[] CreateParameters(
        ParameterExpression resolverContext,
        ParameterInfo[] parameters,
        IReadOnlyDictionary<ParameterInfo, string> argumentNameLookup,
        IReadOnlyList<IParameterExpressionBuilder> parameterExpressionBuilders)
    {
        var parameterResolvers = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var builder = GetParameterExpressionBuilder(parameter, parameterExpressionBuilders);

            var context = new ParameterExpressionBuilderContext(
                parameter,
                resolverContext,
                argumentNameLookup);

            parameterResolvers[i] = builder.Build(context);
        }

        return parameterResolvers;
    }

    private IParameterExpressionBuilder GetParameterExpressionBuilder(
        ParameterInfo parameter,
        IReadOnlyList<IParameterExpressionBuilder> fieldParameterExpressionBuilders)
    {
        if (fieldParameterExpressionBuilders.Count == 0 &&
            _cache.TryGetValue(parameter, out var cached))
        {
            return cached;
        }

        if (fieldParameterExpressionBuilders.Count > 0)
        {
            foreach (var builder in fieldParameterExpressionBuilders)
            {
                if (!builder.IsDefaultHandler && builder.CanHandle(parameter))
                {
                    _cache.TryAdd(parameter, builder);
                    return builder;
                }
            }
        }

        foreach (var builder in _parameterExpressionBuilders)
        {
            if (builder.CanHandle(parameter))
            {
                _cache.TryAdd(parameter, builder);
                return builder;
            }
        }

        if (fieldParameterExpressionBuilders.Count > 0)
        {
            foreach (var builder in fieldParameterExpressionBuilders)
            {
                if (builder.IsDefaultHandler && builder.CanHandle(parameter))
                {
                    _cache.TryAdd(parameter, builder);
                    return builder;
                }
            }
        }

        if (_defaultParameterExpressionBuilders.Count > 0)
        {
            foreach (var builder in _defaultParameterExpressionBuilders)
            {
                if (builder.CanHandle(parameter))
                {
                    _cache.TryAdd(parameter, builder);
                    return builder;
                }
            }
        }

        _cache.TryAdd(parameter, _defaultExprBuilder);
        return _defaultExprBuilder;
    }

    public void Dispose()
    {
        _cache.Clear();
    }
}
