using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.ApolloFederation.FederationContextData;

namespace HotChocolate.ApolloFederation.Types;

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

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReference(
        FieldResolverDelegate fieldResolver)
        => ResolveReference(fieldResolver, Array.Empty<string[]>());

    /// <inheritdoc cref="IEntityResolverDescriptor{T}"/>
    public IObjectTypeDescriptor ResolveReferenceWith(
        Expression<Func<TEntity, object?>> method)
        => ResolveReferenceWith<TEntity>(method);

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReferenceWith<TResolver>(
        Expression<Func<TResolver, object?>> method)
    {
        ArgumentNullException.ThrowIfNull(method);

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
    public IObjectTypeDescriptor ResolveReferenceWith(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        var argumentBuilder = new ReferenceResolverArgumentExpressionBuilder();

        var resolver =
            Context.ResolverCompiler.CompileResolve(
                method,
                sourceType: typeof(object),
                resolverType: method.DeclaringType ?? typeof(object),
                parameterExpressionBuilders: new IParameterExpressionBuilder[] { argumentBuilder, });

        return ResolveReference(resolver.Resolver!, argumentBuilder.Required);
    }

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReferenceWith<TResolver>()
        => ResolveReferenceWith(typeof(TResolver));

    /// <inheritdoc cref="IEntityResolverDescriptor"/>
    public IObjectTypeDescriptor ResolveReferenceWith(Type type)
        => ResolveReferenceWith(
            Context.TypeInspector.GetNodeResolverMethod(
                Definition.EntityType ?? type,
                type)!);

    private IObjectTypeDescriptor ResolveReference(
        FieldResolverDelegate fieldResolver,
        IReadOnlyList<string[]> required)
    {
        ArgumentNullException.ThrowIfNull(fieldResolver);

        ArgumentNullException.ThrowIfNull(required);

        Definition.ResolverDefinition = new(fieldResolver, required);
        return _typeDescriptor;
    }

    protected internal override EntityResolverDefinition Definition { get; protected set; } = new();
}
