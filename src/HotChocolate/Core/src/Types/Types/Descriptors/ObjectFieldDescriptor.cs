using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static System.Reflection.BindingFlags;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Descriptors;

public class ObjectFieldDescriptor
    : OutputFieldDescriptorBase<ObjectFieldConfiguration>
    , IObjectFieldDescriptor
{
    private bool _argumentsInitialized;
    private ParameterInfo[] _parameterInfos = [];

    /// <summary>
    /// Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        string fieldName)
        : base(context)
    {
        Configuration.Name = fieldName;
        Configuration.ResultType = typeof(object);
        Configuration.IsParallelExecutable = context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        MemberInfo member,
        Type sourceType,
        Type? resolverType = null)
        : base(context)
    {
        var naming = context.Naming;
        Configuration.Member = member ?? throw new ArgumentNullException(nameof(member));
        Configuration.Name = naming.GetMemberName(member, MemberKind.ObjectField);
        Configuration.Description = naming.GetMemberDescription(member, MemberKind.ObjectField);
        Configuration.Type = context.TypeInspector.GetOutputReturnTypeRef(member);
        Configuration.SourceType = sourceType;
        Configuration.ResolverType = resolverType == sourceType ? null : resolverType;
        Configuration.IsParallelExecutable = context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;

        if (naming.IsDeprecated(member, out var reason))
        {
            Deprecated(reason);
        }

        switch (member)
        {
            case MethodInfo m:
                _parameterInfos = context.TypeInspector.GetParameters(m);
                Parameters = _parameterInfos.ToDictionary(t => t.Name!, StringComparer.Ordinal);
                Configuration.ResultType = m.ReturnType;
                break;

            case PropertyInfo p:
                Configuration.ResultType = p.PropertyType;
                break;
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        LambdaExpression expression,
        Type sourceType,
        Type? resolverType = null)
        : base(context)
    {
        Configuration.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Configuration.SourceType = sourceType;
        Configuration.ResolverType = resolverType;
        Configuration.IsParallelExecutable = context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;

        var member = expression.TryExtractCallMember();

        if (member is not null)
        {
            var naming = context.Naming;
            Configuration.Name = naming.GetMemberName(member, MemberKind.ObjectField);
            Configuration.Description = naming.GetMemberDescription(member, MemberKind.ObjectField);
            Configuration.Type = context.TypeInspector.GetOutputReturnTypeRef(member);

            if (naming.IsDeprecated(member, out var reason))
            {
                Deprecated(reason);
            }

            switch (member)
            {
                case MethodInfo m:
                    Configuration.ResultType = m.ReturnType;
                    break;

                case PropertyInfo p:
                    Configuration.ResultType = p.PropertyType;
                    break;
            }
        }
        else
        {
            Configuration.Type = context.TypeInspector.GetOutputTypeRef(expression.ReturnType);
            Configuration.ResultType = expression.ReturnType;
        }
    }

    /// <summary>
    ///  Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        ObjectFieldConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal override ObjectFieldConfiguration Configuration { get; protected set; } = new();

    /// <inheritdoc />
    protected override void OnCreateConfiguration(ObjectFieldConfiguration definition)
    {
        Context.Descriptors.Push(this);

        var member = definition.ResolverMember ?? definition.Member;

        if (!Configuration.ConfigurationsAreApplied)
        {
            DescriptorAttributeHelper.ApplyConfiguration(
                Context,
                this,
                member);

            Configuration.ConfigurationsAreApplied = true;
        }

        base.OnCreateConfiguration(definition);

        CompleteArguments(definition);

        if (!definition.HasStreamResult
            && definition.ResultType?.IsGenericType is true
            && definition.ResultType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            definition.HasStreamResult = true;
        }

        Context.Descriptors.Pop();
    }

    private void CompleteArguments(ObjectFieldConfiguration definition)
    {
        if (!_argumentsInitialized)
        {
            if (definition.SubscribeWith is not null)
            {
                var ownerType = definition.ResolverType ?? definition.SourceType;

                var subscribeMember = ownerType?.GetMember(
                    definition.SubscribeWith,
                    Public | NonPublic | Instance | Static)[0];

                if (subscribeMember is MethodInfo subscribeMethod)
                {
                    var subscribeParameters = Context.TypeInspector.GetParameters(subscribeMethod);
                    var parameterLength = _parameterInfos.Length + subscribeParameters.Length;
                    var parameters = new ParameterInfo[parameterLength];

                    _parameterInfos.CopyTo(parameters, 0);
                    subscribeParameters.CopyTo(parameters, _parameterInfos.Length);
                    _parameterInfos = parameters;

                    var parameterLookup = Parameters.ToDictionary(
                        t => t.Key,
                        t => t.Value,
                        StringComparer.Ordinal);
                    Parameters = parameterLookup;

                    foreach (var parameter in subscribeParameters)
                    {
                        parameterLookup.TryAdd(parameter.Name!, parameter);
                    }
                }
            }

            if (Parameters.Count > 0)
            {
                Context.ResolverCompiler.ApplyConfiguration(
                    _parameterInfos,
                    this);

                FieldDescriptorUtilities.DiscoverArguments(
                    Context,
                    definition.Arguments,
                    definition.Member,
                    _parameterInfos,
                    definition.GetParameterExpressionBuilders());

                foreach (var parameter in _parameterInfos)
                {
                    if (!parameter.IsDefined(typeof(ParentAttribute)))
                    {
                        continue;
                    }

                    var requirements = parameter.GetCustomAttribute<ParentAttribute>()?.Requires;
                    if (!(requirements?.Length > 0))
                    {
                        continue;
                    }

                    Configuration.Flags |= CoreFieldFlags.WithRequirements;
                    Configuration.Features.Set(new FieldRequirementFeature(requirements, parameter.ParameterType));
                }
            }

            _argumentsInitialized = true;
        }
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Name(string value)
    {
        base.Name(value);
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Description(string? value)
    {
        base.Description(value);
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Deprecated(string? reason)
    {
        base.Deprecated(reason);
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Deprecated()
    {
        base.Deprecated();
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Type<TOutputType>()
        where TOutputType : class, IOutputType
    {
        base.Type<TOutputType>();
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Type<TOutputType>(TOutputType outputType)
        where TOutputType : class, IOutputType
    {
        base.Type(outputType);
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Type(ITypeNode typeNode)
    {
        base.Type(typeNode);
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Type(Type type)
    {
        base.Type(type);
        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor StreamResult(bool hasStreamResult = true)
    {
        Configuration.HasStreamResult = hasStreamResult;
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Argument(
        string argumentName,
        Action<IArgumentDescriptor> argumentDescriptor)
    {
        base.Argument(argumentName, argumentDescriptor);
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Ignore(bool ignore = true)
    {
        base.Ignore(ignore);
        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor Resolve(FieldResolverDelegate fieldResolver)
    {
        ArgumentNullException.ThrowIfNull(fieldResolver);

        Configuration.Resolver = fieldResolver;
        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor Resolve(
        FieldResolverDelegate fieldResolver,
        Type? resultType)
    {
        ArgumentNullException.ThrowIfNull(fieldResolver);

        Configuration.Resolver = fieldResolver;

        if (resultType is not null)
        {
            Configuration.SetMoreSpecificType(
                Context.TypeInspector.GetType(resultType),
                TypeContext.Output);

            if (resultType.IsGenericType)
            {
                var resultTypeDef = resultType.GetGenericTypeDefinition();

                var clrResultType = resultTypeDef == typeof(NamedRuntimeType<>)
                    ? resultType.GetGenericArguments()[0]
                    : resultType;

                if (!clrResultType.IsSchemaType())
                {
                    Configuration.ResultType = clrResultType;
                }
            }
        }

        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor ResolveWith<TResolver>(
        Expression<Func<TResolver, object?>> propertyOrMethod)
    {
        ArgumentNullException.ThrowIfNull(propertyOrMethod);

        return ResolveWithInternal(propertyOrMethod.ExtractMember(), typeof(TResolver));
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor ResolveWith(MemberInfo propertyOrMethod)
    {
        ArgumentNullException.ThrowIfNull(propertyOrMethod);

        return ResolveWithInternal(propertyOrMethod, propertyOrMethod.DeclaringType);
    }

    private IObjectFieldDescriptor ResolveWithInternal(
        MemberInfo propertyOrMethod,
        Type? resolverType)
    {
        if (resolverType?.IsAbstract is true)
        {
            throw new ArgumentException(
                string.Format(
                    ObjectTypeDescriptor_ResolveWith_NonAbstract,
                    resolverType.FullName),
                nameof(resolverType));
        }

        if (propertyOrMethod is PropertyInfo or MethodInfo)
        {
            Configuration.SetMoreSpecificType(
                Context.TypeInspector.GetReturnType(propertyOrMethod),
                TypeContext.Output);

            Configuration.ResolverType = resolverType;
            Configuration.ResolverMember = propertyOrMethod;
            Configuration.Resolver = null;
            Configuration.ResultType = propertyOrMethod.GetReturnType();

            if (propertyOrMethod is MethodInfo m)
            {
                _parameterInfos = Context.TypeInspector.GetParameters(m);
                Parameters = _parameterInfos.ToDictionary(t => t.Name!, StringComparer.Ordinal);
            }

            return this;
        }

        throw new ArgumentException(
            ObjectTypeDescriptor_MustBePropertyOrMethod,
            nameof(propertyOrMethod));
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor Subscribe(SubscribeResolverDelegate subscribeResolver)
    {
        Configuration.SubscribeResolver = subscribeResolver;
        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor Use(FieldMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        Configuration.MiddlewareConfigurations.Add(new(middleware));
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Directive<T>()
        where T : class, new()
    {
        base.Directive<T>();
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor ParentRequires<TParent>(Expression<Func<TParent, object>> selector)
        => ParentRequires<TParent>(ExpressionSelectionSetFormatter.Format(selector));

    /// <inheritdoc />
    public IObjectFieldDescriptor ParentRequires<TParent>(string? requires)
    {
        if (!(requires?.Length > 0))
        {
            Configuration.Flags &= ~CoreFieldFlags.WithRequirements;
            Configuration.Features.Set<FieldRequirementFeature>(null);
            return this;
        }

        Configuration.Flags |= CoreFieldFlags.WithRequirements;
        Configuration.Features.Set(new FieldRequirementFeature(requires, typeof(TParent)));
        return this;
    }

    public IObjectFieldDescriptor ParentRequires(string? requires)
    {
        if (!(requires?.Length > 0))
        {
            Configuration.Flags &= ~CoreFieldFlags.WithRequirements;
            Configuration.Features.Set<FieldRequirementFeature>(null);
            return this;
        }

        Configuration.Flags |= CoreFieldFlags.WithRequirements;
        Configuration.Features.Set(new FieldRequirementFeature(requires, Configuration.SourceType));
        return this;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="fieldName">The name of the field</param>
    /// <returns>An instance of <see cref="DirectiveArgumentDescriptor"/></returns>
    public static ObjectFieldDescriptor New(
        IDescriptorContext context,
        string fieldName)
        => new(context, fieldName);

    /// <summary>
    /// Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="member">The member this field represents</param>
    /// <param name="sourceType">The type of the member</param>
    /// <param name="resolverType">The resolved type</param>
    /// <returns></returns>
    public static ObjectFieldDescriptor New(
        IDescriptorContext context,
        MemberInfo member,
        Type sourceType,
        Type? resolverType = null)
        => new(context, member, sourceType, resolverType);

    /// <summary>
    /// Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="expression">The expression this field is based on</param>
    /// <param name="sourceType">The type of the member</param>
    /// <param name="resolverType">The resolved type</param>
    /// <returns></returns>
    public static ObjectFieldDescriptor New(
        IDescriptorContext context,
        LambdaExpression expression,
        Type sourceType,
        Type? resolverType = null)
        => new(context, expression, sourceType, resolverType);

    /// <summary>
    /// Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    /// <param name="context">The descriptor context</param>
    /// <param name="definition">The definition of the field</param>
    /// <returns></returns>
    public static ObjectFieldDescriptor From(
        IDescriptorContext context,
        ObjectFieldConfiguration definition)
        => new(context, definition);

    public static class ExpressionSelectionSetFormatter
    {
        public static string Format<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return ProcessExpression(expression.Body).Trim();
        }

        private static string ProcessExpression(Expression? expression)
        {
            if (expression is null)
            {
                return string.Empty;
            }

            switch (expression)
            {
                case UnaryExpression { NodeType: ExpressionType.Convert } unaryExpr:
                    return ProcessExpression(unaryExpr.Operand);

                case MemberExpression memberExpr:
                    var parent = ProcessExpression(memberExpr.Expression);
                    return string.IsNullOrEmpty(parent)
                        ? memberExpr.Member.Name
                        : $"{parent} {{ {memberExpr.Member.Name} }}";

                case NewExpression newExpr:
                    return string.Join(" ", newExpr.Arguments.Select(ProcessExpression));

                case MemberInitExpression memberInitExpr:
                    return string.Join(" ", memberInitExpr.Bindings.OfType<MemberAssignment>()
                        .Select(b => b.Member.Name + ProcessExpression(b.Expression)));

                case MethodCallExpression { Method.Name: "Select" } methodCallExpr:
                    if (methodCallExpr.Arguments is [_, UnaryExpression { Operand: LambdaExpression lambda }])
                    {
                        return $"{{ {ProcessExpression(lambda.Body)} }}";
                    }
                    break;
            }

            return string.Empty;
        }
    }
}
