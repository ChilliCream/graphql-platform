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
    : OutputFieldDescriptorBase<InterfaceFieldDefinition>
    , IInterfaceFieldDescriptor
{
    private ParameterInfo[] _parameterInfos = [];
    private bool _argumentsInitialized;

    protected internal InterfaceFieldDescriptor(
        IDescriptorContext context,
        string fieldName)
        : base(context)
    {
        Definition.Name = fieldName;
    }

    protected internal InterfaceFieldDescriptor(
        IDescriptorContext context,
        InterfaceFieldDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal InterfaceFieldDescriptor(
        IDescriptorContext context,
        MemberInfo member)
        : base(context)
    {
        var naming = context.Naming;

        Definition.Member = member ?? throw new ArgumentNullException(nameof(member));
        Definition.Name = naming.GetMemberName(member, MemberKind.InterfaceField);
        Definition.Description = naming.GetMemberDescription(member, MemberKind.InterfaceField);
        Definition.Type = context.TypeInspector.GetOutputReturnTypeRef(member);

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

    protected internal override InterfaceFieldDefinition Definition { get; protected set; } = new();

    protected override void OnCreateDefinition(InterfaceFieldDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (Definition is { AttributesAreApplied: false, Member: not null, })
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Definition.Member);
            Definition.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        CompleteArguments(definition);

        Context.Descriptors.Pop();
    }

    private void CompleteArguments(InterfaceFieldDefinition definition)
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
        Definition.HasStreamResult = hasStreamResult;
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

        Definition.Resolver = fieldResolver;
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

    public IInterfaceFieldDescriptor Use(FieldMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        Definition.MiddlewareDefinitions.Add(new(middleware));
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
        InterfaceFieldDefinition definition)
        => new(context, definition);
}
