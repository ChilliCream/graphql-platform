#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types.Descriptors;

public class InterfaceFieldDescriptor
    : OutputFieldDescriptorBase<InterfaceFieldConfiguration>
    , IInterfaceFieldDescriptor
{
    private ParameterInfo[] _parameterInfos = [];
    private bool _argumentsInitialized;

    protected internal InterfaceFieldDescriptor(
        IDescriptorContext context,
        string fieldName)
        : base(context)
    {
        Configuration.Name = fieldName;
    }

    protected internal InterfaceFieldDescriptor(
        IDescriptorContext context,
        InterfaceFieldConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal InterfaceFieldDescriptor(
        IDescriptorContext context,
        MemberInfo member)
        : base(context)
    {
        var naming = context.Naming;

        Configuration.Member = member ?? throw new ArgumentNullException(nameof(member));
        Configuration.Name = naming.GetMemberName(member, MemberKind.InterfaceField);
        Configuration.Description = naming.GetMemberDescription(member, MemberKind.InterfaceField);
        Configuration.Type = context.TypeInspector.GetOutputReturnTypeRef(member);

        if (naming.IsDeprecated(member, out var reason))
        {
            Deprecated(reason);
        }

        if (member is MethodInfo m)
        {
            _parameterInfos = m.GetParameters();
            Parameters = _parameterInfos.ToDictionary(t => t.Name!, StringComparer.Ordinal);
        }
    }

    protected internal override InterfaceFieldConfiguration Configuration { get; protected set; } = new();

    protected override void OnCreateDefinition(InterfaceFieldConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (Configuration is { AttributesAreApplied: false, Member: not null, })
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Configuration.Member);
            Configuration.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        CompleteArguments(definition);

        Context.Descriptors.Pop();
    }

    private void CompleteArguments(InterfaceFieldConfiguration definition)
    {
        if (!_argumentsInitialized && Parameters.Any())
        {
            FieldDescriptorUtilities.DiscoverArguments(
                Context,
                definition.Arguments,
                definition.Member,
                _parameterInfos,
                definition.GetParameterExpressionBuilders());
            _argumentsInitialized = true;
        }
    }

    public new IInterfaceFieldDescriptor Name(string name)
    {
        base.Name(name);
        return this;
    }

    public new IInterfaceFieldDescriptor Description(string? description)
    {
        base.Description(description);
        return this;
    }

    public new IInterfaceFieldDescriptor Deprecated(string? reason)
    {
        base.Deprecated(reason);
        return this;
    }

    public new IInterfaceFieldDescriptor Deprecated()
    {
        base.Deprecated();
        return this;
    }

    public new IInterfaceFieldDescriptor Type<TOutputType>()
        where TOutputType : IOutputType
    {
        base.Type<TOutputType>();
        return this;
    }

    public new IInterfaceFieldDescriptor Type<TOutputType>(TOutputType outputType)
        where TOutputType : class, IOutputType
    {
        base.Type(outputType);
        return this;
    }

    public new IInterfaceFieldDescriptor Type(ITypeNode type)
    {
        base.Type(type);
        return this;
    }

    public new IInterfaceFieldDescriptor Type(Type type)
    {
        base.Type(type);
        return this;
    }

    public IInterfaceFieldDescriptor StreamResult(bool hasStreamResult = true)
    {
        Configuration.HasStreamResult = hasStreamResult;
        return this;
    }

    public new IInterfaceFieldDescriptor Argument(
        string argumentName,
        Action<IArgumentDescriptor> argumentDescriptor)
    {
        base.Argument(argumentName, argumentDescriptor);
        return this;
    }

    public new IInterfaceFieldDescriptor Ignore(bool ignore = true)
    {
        base.Ignore(ignore);
        return this;
    }

    public IInterfaceFieldDescriptor Resolve(FieldResolverDelegate fieldResolver)
    {
        if (fieldResolver is null)
        {
            throw new ArgumentNullException(nameof(fieldResolver));
        }

        Configuration.Resolver = fieldResolver;
        return this;
    }

    public IInterfaceFieldDescriptor Resolve(
        FieldResolverDelegate fieldResolver,
        Type? resultType)
    {
        if (fieldResolver is null)
        {
            throw new ArgumentNullException(nameof(fieldResolver));
        }

        Configuration.Resolver = fieldResolver;

        if (resultType is not null)
        {
            Configuration.SetMoreSpecificType(
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
                    Configuration.ResultType = clrResultType;
                }
            }
        }

        return this;
    }

    public IInterfaceFieldDescriptor ResolveWith<TResolver>(
        Expression<Func<TResolver, object>> propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        return ResolveWithInternal(propertyOrMethod.ExtractMember(), typeof(TResolver));
    }

    public IInterfaceFieldDescriptor ResolveWith(MemberInfo propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        return ResolveWithInternal(propertyOrMethod, propertyOrMethod.DeclaringType);
    }

    private IInterfaceFieldDescriptor ResolveWithInternal(
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
                _parameterInfos = m.GetParameters();
                Parameters = _parameterInfos.ToDictionary(t => t.Name!, StringComparer.Ordinal);
            }

            return this;
        }

        throw new ArgumentException(
            ObjectTypeDescriptor_MustBePropertyOrMethod,
            nameof(propertyOrMethod));
    }

    public IInterfaceFieldDescriptor Use(FieldMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        Configuration.MiddlewareDefinitions.Add(new(middleware));
        return this;
    }

    public new IInterfaceFieldDescriptor Directive<T>(T directive)
        where T : class
    {
        base.Directive(directive);
        return this;
    }

    public new IInterfaceFieldDescriptor Directive<T>()
        where T : class, new()
    {
        base.Directive<T>();
        return this;
    }

    public new IInterfaceFieldDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    public static InterfaceFieldDescriptor New(
        IDescriptorContext context,
        string fieldName)
        => new(context, fieldName);

    public static InterfaceFieldDescriptor New(
        IDescriptorContext context,
        MemberInfo member)
        => new(context, member);

    public static InterfaceFieldDescriptor From(
        IDescriptorContext context,
        InterfaceFieldConfiguration definition)
        => new(context, definition);
}
