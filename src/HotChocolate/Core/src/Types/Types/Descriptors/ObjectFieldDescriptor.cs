using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class ObjectFieldDescriptor
    : OutputFieldDescriptorBase<ObjectFieldDefinition>
    , IObjectFieldDescriptor
{
    private bool _argumentsInitialized;
    private ParameterInfo[] _parameterInfos = Array.Empty<ParameterInfo>();

    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        NameString fieldName)
        : base(context)
    {
        Definition.Name = fieldName.EnsureNotEmpty(nameof(fieldName));
        Definition.ResultType = typeof(object);
        Definition.IsParallelExecutable =
            context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;
    }

    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        MemberInfo member,
        Type sourceType,
        Type? resolverType = null)
        : base(context)
    {
        Definition.Member = member ??
            throw new ArgumentNullException(nameof(member));
        Definition.Name = context.Naming.GetMemberName(
            member,
            MemberKind.ObjectField);
        Definition.Description = context.Naming.GetMemberDescription(
            member,
            MemberKind.ObjectField);
        Definition.Type = context.TypeInspector.GetOutputReturnTypeRef(member);
        Definition.SourceType = sourceType;
        Definition.ResolverType = resolverType == sourceType
            ? null
            : resolverType;
        Definition.IsParallelExecutable =
            context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;

        if (context.Naming.IsDeprecated(member, out var reason))
        {
            Deprecated(reason);
        }

        if (member is MethodInfo m)
        {
            _parameterInfos = m.GetParameters();
            Parameters = _parameterInfos.ToDictionary(t => new NameString(t.Name!));
            Definition.ResultType = m.ReturnType;
        }
        else if (member is PropertyInfo p)
        {
            Definition.ResultType = p.PropertyType;
        }
    }

    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        LambdaExpression expression,
        Type sourceType,
        Type? resolverType = null)
        : base(context)
    {
        Definition.Expression = expression
            ?? throw new ArgumentNullException(nameof(expression));
        Definition.SourceType = sourceType;
        Definition.ResolverType = resolverType;
        Definition.IsParallelExecutable =
            context.Options.DefaultResolverStrategy is ExecutionStrategy.Parallel;

        MemberInfo? member = expression.TryExtractCallMember();

        if (member is not null)
        {
            Definition.Name = context.Naming.GetMemberName(
                member,
                MemberKind.ObjectField);
            Definition.Description = context.Naming.GetMemberDescription(
                member,
                MemberKind.ObjectField);
            Definition.Type = context.TypeInspector.GetOutputReturnTypeRef(member);

            if (context.Naming.IsDeprecated(member, out var reason))
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

    protected ObjectFieldDescriptor(
        IDescriptorContext context,
        ObjectFieldDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal override ObjectFieldDefinition Definition { get; protected set; } = new();

    protected override void OnCreateDefinition(ObjectFieldDefinition definition)
    {
        MemberInfo? member = definition.ResolverMember ?? definition.Member;

        if (!Definition.AttributesAreApplied && member is not null)
        {
            Context.TypeInspector.ApplyAttributes(Context, this, member);
            Definition.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        CompleteArguments(definition);
    }

    private void CompleteArguments(ObjectFieldDefinition definition)
    {
        if (!_argumentsInitialized && Parameters.Count > 0)
        {
            Context.ResolverCompiler.ApplyConfiguration(
                _parameterInfos,
                this);

            FieldDescriptorUtilities.DiscoverArguments(
                Context,
                definition.Arguments,
                definition.Member);

            _argumentsInitialized = true;
        }
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor SyntaxNode(FieldDefinitionNode? fieldDefinition)
    {
        base.SyntaxNode(fieldDefinition);
        return this;
    }

    /// <inheritdoc />
    public new IObjectFieldDescriptor Name(NameString value)
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
    [Obsolete("Use `Deprecated`.")]
    public IObjectFieldDescriptor DeprecationReason(string? reason) =>
        Deprecated(reason);

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
    public new IObjectFieldDescriptor Type<TOutputType>(
        TOutputType outputType)
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
    public new IObjectFieldDescriptor Argument(
        NameString argumentName,
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
    public IObjectFieldDescriptor Resolver(
        FieldResolverDelegate fieldResolver) =>
        Resolve(fieldResolver);

    /// <inheritdoc />
    public IObjectFieldDescriptor Resolver(
        FieldResolverDelegate fieldResolver,
        Type? resultType) =>
        Resolve(fieldResolver, resultType);

    /// <inheritdoc />
    public IObjectFieldDescriptor Resolve(
        FieldResolverDelegate fieldResolver)
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
                Type resultTypeDef = resultType.GetGenericTypeDefinition();

                Type clrResultType = resultTypeDef == typeof(NativeType<>)
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

        return ResolveWith(propertyOrMethod.ExtractMember());
    }

    public IObjectFieldDescriptor ResolveWith(
        MemberInfo propertyOrMethod)
    {
        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        if (propertyOrMethod is PropertyInfo or MethodInfo)
        {
            Definition.SetMoreSpecificType(
                Context.TypeInspector.GetReturnType(propertyOrMethod),
                TypeContext.Output);

            Definition.ResolverType = propertyOrMethod.DeclaringType;
            Definition.ResolverMember = propertyOrMethod;
            Definition.Resolver = null;
            Definition.ResultType = propertyOrMethod.GetReturnType();

            if (propertyOrMethod is MethodInfo m)
            {
                _parameterInfos = m.GetParameters();

                var parameters = new Dictionary<NameString, ParameterInfo>();

                foreach (ParameterInfo parameterInfo in _parameterInfos)
                {
                    parameters.Add(new NameString(parameterInfo.Name!), parameterInfo);
                }

                Parameters = parameters;
            }

            return this;
        }

        throw new ArgumentException(
            TypeResources.ObjectTypeDescriptor_MustBePropertyOrMethod,
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
        NameString name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    public static ObjectFieldDescriptor New(
        IDescriptorContext context,
        NameString fieldName) =>
        new(context, fieldName);

    public static ObjectFieldDescriptor New(
        IDescriptorContext context,
        MemberInfo member,
        Type sourceType,
        Type? resolverType = null) =>
        new(context, member, sourceType, resolverType);

    public static ObjectFieldDescriptor New(
        IDescriptorContext context,
        LambdaExpression expression,
        Type sourceType,
        Type? resolverType = null) =>
        new(context, expression, sourceType, resolverType);

    public static ObjectFieldDescriptor From(
        IDescriptorContext context,
        ObjectFieldDefinition definition) =>
        new(context, definition);
}
