using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

namespace HotChocolate.ApolloFederation;

public class EntityResolverDescriptor
    : DescriptorBase<EntityResolverDefinition>
    , IEntityResolverDescriptor
{
    private readonly IObjectTypeDescriptor _typeDescriptor;

    public EntityResolverDescriptor(
        IObjectTypeDescriptor descriptor,
        Type? resolvedEntityType = null)
        : base(descriptor.Extend().Context)
    {
        _typeDescriptor = descriptor;

        _typeDescriptor
            .Extend()
            .OnBeforeCreate(OnCompleteDefinition);

        Definition.ResolvedEntityType = resolvedEntityType;
    }

    private void OnCompleteDefinition(ObjectTypeDefinition definition)
    {
        if (Definition.Resolver is not null)
        {
            definition.ContextData[WellKnownContextData.EntityResolver] = Definition.Resolver;
        }
    }

    protected internal override EntityResolverDefinition Definition { get; protected set; } = new();

    public IObjectTypeDescriptor ResolveEntity(FieldResolverDelegate fieldResolver)
    {
        Definition.Resolver = fieldResolver ??
            throw new ArgumentNullException(nameof(fieldResolver));
        return _typeDescriptor;
    }

    public IObjectTypeDescriptor ResolveEntityWith<TResolver>(
        Expression<Func<TResolver, object>> method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        MemberInfo member = method.TryExtractMember();

        if (member is MethodInfo m)
        {
            FieldResolverDelegates resolver =
                Context.ResolverCompiler.CompileResolve(
                    m,
                    sourceType: typeof(object),
                    resolverType: typeof(TResolver));
            return ResolveEntity(resolver.Resolver!);
        }

        throw new ArgumentException(
            FederationResources.EntityResolver_MustBeMethod,
            nameof(member));
    }

    public IObjectTypeDescriptor ResolveEntityWith<TResolver>() =>
        ResolveEntityWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.ResolvedEntityType ?? typeof(TResolver),
                typeof(TResolver))!);

    public IObjectTypeDescriptor ResolveEntityWith(MethodInfo method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        FieldResolverDelegates resolver =
            Context.ResolverCompiler.CompileResolve(
                method,
                sourceType: typeof(object),
                resolverType: method.DeclaringType ?? typeof(object),
                parameterExpressionBuilders: new IParameterExpressionBuilder[]
                {
                    new LocalStateParameterExpressionBuilder()
                });

        return ResolveEntity(resolver.Resolver!);
    }

    public IObjectTypeDescriptor ResolveEntityWith(Type type) =>
        ResolveEntityWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.ResolvedEntityType ?? type,
                type)!);
}

internal sealed class LocalStateParameterExpressionBuilder
    : ScopedStateParameterExpressionBuilder
{
    public override ArgumentKind Kind => ArgumentKind.LocalState;

    protected override PropertyInfo ContextDataProperty { get; } =
        ContextType.GetProperty(nameof(IResolverContext.LocalContextData))!;

    protected override MethodInfo SetStateMethod { get; } =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetLocalState))!;

    protected override MethodInfo SetStateGenericMethod { get; } =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetLocalStateGeneric))!;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(LocalStateAttribute));

    protected override string GetKey(ParameterInfo parameter) => "data";

    public override Expression Build(ParameterInfo parameter, Expression context)
    {
        var path = parameter.GetCustomAttribute<MapAttribute>() is { } attr
            ? attr.Path.Split('.')
            : new[] { parameter.Name! };

        ConstantExpression key = Expression.Constant("data", typeof(string));
        Expression value = BuildGetter(parameter, key, context, typeof(ObjectValueNode));

        return null;
    }


}
