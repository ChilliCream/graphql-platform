using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.ApolloFederation.Helpers;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;

namespace HotChocolate.ApolloFederation.Descriptors;

/// <summary>
/// The entity descriptor allows to specify a reference resolver.
/// </summary>
public sealed class EntityResolverDescriptor<TEntity>
    : DescriptorBase<EntityResolverDefinition>
    , IEntityResolverDescriptor
    , IEntityResolverDescriptor<TEntity>
{
    private readonly IObjectTypeDescriptor _typeDescriptor;

    internal EntityResolverDescriptor(
        IObjectTypeDescriptor<TEntity> descriptor)
        : this((ObjectTypeDescriptor)descriptor, typeof(TEntity))
    {
    }

    internal EntityResolverDescriptor(
        IObjectTypeDescriptor descriptor,
        Type? entityType = null)
        : base(descriptor.Extend().Context)
    {
        _typeDescriptor = descriptor;

        _typeDescriptor
            .Extend()
            .OnBeforeCreate(OnCompleteDefinition);

        Definition.EntityType = entityType;
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
                        Definition.ResolverDefinition.Value,
                    });
            }
        }
    }

    protected internal override EntityResolverDefinition Definition { get; protected set; } = new();

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReference(
        FieldResolverDelegate fieldResolver)
        => ResolveReference(fieldResolver, Array.Empty<string[]>());

    private IObjectTypeDescriptor ResolveReference(
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

    /// <inheritdoc cref="IEntityResolverDescriptor{T}"/>
    public IObjectTypeDescriptor ResolveReferenceWith(
        Expression<Func<TEntity, object?>> method)
        => ResolveReferenceWith<TEntity>(method);

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReferenceWith<TResolver>(
        Expression<Func<TResolver, object?>> method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var member = method.TryExtractMember(true);

        if (member is MethodInfo m)
        {
            return ResolveReferenceWith(m);
        }

        throw new ArgumentException(
            FederationResources.EntityResolver_MustBeMethod,
            nameof(member));
    }

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReferenceWith<TResolver>()
        => ResolveReferenceWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.EntityType ?? typeof(TResolver),
                typeof(TResolver))!);

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReferenceWith(MethodInfo method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var argumentBuilder = new ReferenceResolverArgumentExpressionBuilder();

        var resolver =
            Context.ResolverCompiler.CompileResolve(
                method,
                sourceType: typeof(object),
                resolverType: method.DeclaringType ?? typeof(object),
                parameterExpressionBuilders: new IParameterExpressionBuilder[] { argumentBuilder });

        return ResolveReference(resolver.Resolver!, argumentBuilder.Required);
    }

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReferenceWith(Type type)
        => ResolveReferenceWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.EntityType ?? type,
                type)!);
}
