using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static System.Reflection.BindingFlags;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.WellKnownContextData;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class ObjectFieldDescriptor
    : OutputFieldDescriptorBase<ObjectFieldDefinition>
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
        Definition.Name = fieldName;
        Definition.ResultType = typeof(object);
        Definition.IsParallelExecutable =
            context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;
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
        Definition.Member = member ?? throw new ArgumentNullException(nameof(member));
        Definition.Name = naming.GetMemberName(member, MemberKind.ObjectField);
        Definition.Description = naming.GetMemberDescription(member, MemberKind.ObjectField);
        Definition.Type = context.TypeInspector.GetOutputReturnTypeRef(member);
        Definition.SourceType = sourceType;
        Definition.ResolverType = resolverType == sourceType
            ? null
            : resolverType;
        Definition.IsParallelExecutable =
            context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;

        if (naming.IsDeprecated(member, out var reason))
        {
            Deprecated(reason);
        }

        if (member is MethodInfo m)
        {
            _parameterInfos = m.GetParameters();
            Parameters = _parameterInfos.ToDictionary(t => t.Name!, StringComparer.Ordinal);
            Definition.ResultType = m.ReturnType;
        }
        else if (member is PropertyInfo p)
        {
            Definition.ResultType = p.PropertyType;
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
        Definition.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        Definition.SourceType = sourceType;
        Definition.ResolverType = resolverType;
        Definition.IsParallelExecutable =
            context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;

        var member = expression.TryExtractCallMember();

        if (member is not null)
        {
            var naming = context.Naming;
            Definition.Name = naming.GetMemberName(member, MemberKind.ObjectField);
            Definition.Description = naming.GetMemberDescription(member, MemberKind.ObjectField);
            Definition.Type = context.TypeInspector.GetOutputReturnTypeRef(member);

            if (naming.IsDeprecated(member, out var reason))
            {
                Deprecated(reason);
            }

            if (member is MethodInfo m)
            {
                Definition.ResultType = m.ReturnType;
            }
            else if (member is PropertyInfo p)
            {
                Definition.ResultType = p.PropertyType;
            }
        }
        else
        {
            Definition.Type = context.TypeInspector.GetOutputTypeRef(expression.ReturnType);
            Definition.ResultType = expression.ReturnType;
        }
    }

    /// <summary>
    ///  Creates a new instance of <see cref="ObjectFieldDescriptor"/>
    /// </summary>
    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        ObjectFieldDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal override ObjectFieldDefinition Definition { get; protected set; } = new();

    /// <inheritdoc />
    protected override void OnCreateDefinition(ObjectFieldDefinition definition)
    {
        Context.Descriptors.Push(this);

        var member = definition.ResolverMember ?? definition.Member;

        if (!Definition.AttributesAreApplied && member is not null)
        {
            Context.TypeInspector.ApplyAttributes(Context, this, member);
            Definition.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        CompleteArguments(definition);

        if (!definition.HasStreamResult &&
            definition.ResultType?.IsGenericType is true &&
            definition.ResultType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            definition.HasStreamResult = true;
        }

        Context.Descriptors.Pop();
    }

    private void CompleteArguments(ObjectFieldDefinition definition)
    {
        if (!_argumentsInitialized)
        {
            if (definition.SubscribeWith is not null)
            {
                var ownerType = definition.ResolverType ?? definition.SourceType;

                if (ownerType is not null)
                {
                    var subscribeMember = ownerType.GetMember(
                        definition.SubscribeWith,
                        Public | NonPublic | Instance | Static)[0];

                    if (subscribeMember is MethodInfo subscribeMethod)
                    {
                        var subscribeParameters = subscribeMethod.GetParameters();
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
                            if (!parameterLookup.ContainsKey(parameter.Name!))
                            {
                                parameterLookup.Add(parameter.Name!, parameter);
                            }
                        }
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

                    Definition.Flags |= FieldFlags.WithRequirements;
                    Definition.ContextData[FieldRequirementsSyntax] = requirements;
                    Definition.ContextData[FieldRequirementsEntity] = parameter.ParameterType;
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
        Definition.HasStreamResult = hasStreamResult;
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
        if (fieldResolver is null)
        {
            throw new ArgumentNullException(nameof(fieldResolver));
        }

        Definition.Resolver = fieldResolver;
        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor Resolve(
        FieldResolverDelegate fieldResolver,
        Type? resultType)
    {
        if (fieldResolver is null)
        {
            throw new ArgumentNullException(nameof(fieldResolver));
        }

        Definition.Resolver = fieldResolver;

        if (resultType is not null)
        {
            Definition.SetMoreSpecificType(
                Context.TypeInspector.GetType(resultType),
                TypeContext.Output);

            if (resultType.IsGenericType)
            {
                var resultTypeDef = resultType.GetGenericTypeDefinition();

                var clrResultType = resultTypeDef == typeof(NativeType<>)
                    ? resultType.GetGenericArguments()[0]
                    : resultType;

                if (!clrResultType.IsSchemaType())
                {
                    Definition.ResultType = clrResultType;
                }
            }
        }

        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor ResolveWith<TResolver>(
        Expression<Func<TResolver, object?>> propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        return ResolveWithInternal(propertyOrMethod.ExtractMember(), typeof(TResolver));
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor ResolveWith(MemberInfo propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

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
            Definition.SetMoreSpecificType(
                Context.TypeInspector.GetReturnType(propertyOrMethod),
                TypeContext.Output);

            Definition.ResolverType = resolverType;
            Definition.ResolverMember = propertyOrMethod;
            Definition.Resolver = null;
            Definition.ResultType = propertyOrMethod.GetReturnType();

            if (propertyOrMethod is MethodInfo m)
            {
                _parameterInfos = m.GetParameters();
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
        Definition.SubscribeResolver = subscribeResolver;
        return this;
    }

    /// <inheritdoc />
    public IObjectFieldDescriptor Use(FieldMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        Definition.MiddlewareDefinitions.Add(new(middleware));
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
    public IObjectFieldDescriptor ParentRequires<TParent>(string? requires)
    {
        if (!(requires?.Length > 0))
        {
            Definition.Flags &= ~FieldFlags.WithRequirements;
            Definition.ContextData.Remove(FieldRequirementsSyntax);
            Definition.ContextData.Remove(FieldRequirementsEntity);
            return this;
        }

        Definition.Flags |= FieldFlags.WithRequirements;
        Definition.ContextData[FieldRequirementsSyntax] = requires;
        Definition.ContextData[FieldRequirementsEntity] = typeof(TParent);
        return this;
    }

    public IObjectFieldDescriptor ParentRequires(string? requires)
    {
        if (!(requires?.Length > 0))
        {
            Definition.Flags &= ~FieldFlags.WithRequirements;
            Definition.ContextData.Remove(FieldRequirementsSyntax);
            Definition.ContextData.Remove(FieldRequirementsEntity);
            return this;
        }

        Definition.Flags |= FieldFlags.WithRequirements;
        Definition.ContextData[FieldRequirementsSyntax] = requires;
        Definition.ContextData[FieldRequirementsEntity] = Definition.SourceType;
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
        ObjectFieldDefinition definition)
        => new(context, definition);
}
