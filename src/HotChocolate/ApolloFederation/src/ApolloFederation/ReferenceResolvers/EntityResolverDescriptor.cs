using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.ApolloFederation.WellKnownContextData;

namespace HotChocolate.ApolloFederation;

public sealed class EntityResolverDescriptor
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
        if (Definition.ResolverDefinition is not null)
        {
            if (definition.ContextData.TryGetValue(EntityResolver, out var value) &&
                value is List<ReferenceResolverDefinition> resolvers)
            {
                resolvers.Add(Definition.ResolverDefinition.Value);
            }
            else
            {
                definition.ContextData.Add(
                    EntityResolver,
                    new List<ReferenceResolverDefinition>
                    {
                        Definition.ResolverDefinition.Value
                    });
            }
        }
    }

    protected internal override EntityResolverDefinition Definition { get; protected set; } = new();

    public IObjectTypeDescriptor ResolveEntity(
        FieldResolverDelegate fieldResolver)
        => ResolveEntity(fieldResolver, Array.Empty<string[]>());

    private IObjectTypeDescriptor ResolveEntity(
        FieldResolverDelegate fieldResolver,
        IReadOnlyList<string[]> required)
    {
        if (fieldResolver is null)
        {
            throw new ArgumentNullException(nameof(fieldResolver));
        }

        if (required is null)
        {
            throw new ArgumentNullException(nameof(required));
        }

        Definition.ResolverDefinition = new(fieldResolver, required);
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

        var argumentBuilder = new ReferenceResolverArgumentExpressionBuilder();

        FieldResolverDelegates resolver =
            Context.ResolverCompiler.CompileResolve(
                method,
                sourceType: typeof(object),
                resolverType: method.DeclaringType ?? typeof(object),
                parameterExpressionBuilders: new IParameterExpressionBuilder[] { argumentBuilder });

        return ResolveEntity(resolver.Resolver!, argumentBuilder.Paths);
    }

    public IObjectTypeDescriptor ResolveEntityWith(Type type) =>
        ResolveEntityWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.ResolvedEntityType ?? type,
                type)!);
}
