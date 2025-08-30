using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionSchemaDefinition : ISchemaDefinition
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly ConcurrentDictionary<string, ImmutableArray<FusionObjectTypeDefinition>> _possibleTypes = new();
    private readonly ConcurrentDictionary<(string, string?), ImmutableArray<Lookup>> _possibleLookups = new();
    private ImmutableArray<FusionUnionTypeDefinition> _unionTypes;
    private IFeatureCollection _features;
    private bool _sealed;

    internal FusionSchemaDefinition(
        string name,
        string? description,
        IServiceProvider services,
        FusionObjectTypeDefinition queryType,
        FusionObjectTypeDefinition? mutationType,
        FusionObjectTypeDefinition? subscriptionType,
        FusionDirectiveCollection directives,
        FusionTypeDefinitionCollection types,
        FusionDirectiveDefinitionCollection directiveDefinitions,
        IFeatureCollection features)
    {
        Name = name;
        Description = description;
        Services = services;
        QueryType = queryType;
        MutationType = mutationType;
        SubscriptionType = subscriptionType;
        Directives = directives;
        Types = types;
        DirectiveDefinitions = directiveDefinitions;
        _features = features;
    }

    public static FusionSchemaDefinition Create(
        DocumentNode document,
        IServiceProvider? services = null,
        IFeatureCollection? features = null)
        => Create(
            ISchemaDefinition.DefaultName,
            document,
            services,
            features);

    public static FusionSchemaDefinition Create(
        string name,
        DocumentNode document,
        IServiceProvider? services = null,
        IFeatureCollection? features = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(document);

        return CompositeSchemaBuilder.Create(name, document, services, features);
    }

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the schema description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the schema services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// The type that query operations will be rooted at.
    /// </summary>
    public FusionObjectTypeDefinition QueryType { get; }

    IObjectTypeDefinition ISchemaDefinition.QueryType => QueryType;

    /// <summary>
    /// If this server supports mutation, the type that
    /// mutation operations will be rooted at.
    /// </summary>
    public FusionObjectTypeDefinition? MutationType { get; }

    IObjectTypeDefinition? ISchemaDefinition.MutationType => MutationType;

    /// <summary>
    /// If this server support subscription, the type that
    /// subscription operations will be rooted at.
    /// </summary>
    public FusionObjectTypeDefinition? SubscriptionType { get; }

    IObjectTypeDefinition? ISchemaDefinition.SubscriptionType => SubscriptionType;

    /// <summary>
    /// Gets all the directive types that are annotated to the schema.
    /// </summary>
    public FusionDirectiveCollection Directives { get; }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    /// <summary>
    /// Gets all the schema types.
    /// </summary>
    public FusionTypeDefinitionCollection Types { get; }

    IReadOnlyTypeDefinitionCollection ISchemaDefinition.Types => Types;

    /// <summary>
    /// Gets all the directive definitions that are supported by this schema.
    /// </summary>
    public FusionDirectiveDefinitionCollection DirectiveDefinitions { get; }

    IReadOnlyDirectiveDefinitionCollection ISchemaDefinition.DirectiveDefinitions
        => DirectiveDefinitions;

    public IFeatureCollection Features => _features;

    public FusionObjectTypeDefinition GetOperationType(OperationType operation)
    {
        var type = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => throw new NotSupportedException()
        };

        if (type is null)
        {
            throw new InvalidOperationException(
                $"The specified operation type `{operation}` is not supported.");
        }

        return type;
    }

    IObjectTypeDefinition ISchemaDefinition.GetOperationType(OperationType operation)
        => GetOperationType(operation);

    public bool TryGetOperationType(
        OperationType operation,
        [NotNullWhen(true)] out FusionObjectTypeDefinition? type)
    {
        type = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => throw new NotSupportedException()
        };
        return type is not null;
    }

    bool ISchemaDefinition.TryGetOperationType(
        OperationType operation,
        [NotNullWhen(true)] out IObjectTypeDefinition? type)
    {
        type = operation switch
        {
            OperationType.Query => QueryType,
            OperationType.Mutation => MutationType,
            OperationType.Subscription => SubscriptionType,
            _ => throw new NotSupportedException()
        };
        return type is not null;
    }

    /// <summary>
    /// Gets the possible object types to
    /// an abstract type (union type or interface type).
    /// </summary>
    /// <param name="abstractType">The abstract type.</param>
    /// <returns>
    /// Returns a collection with all possible object types
    /// for the given abstract type.
    /// </returns>
    public ImmutableArray<FusionObjectTypeDefinition> GetPossibleTypes(ITypeDefinition abstractType)
    {
        if (abstractType.Kind is not TypeKind.Union and not TypeKind.Interface and not TypeKind.Object)
        {
            throw new ArgumentException(
                "The specified type is not an abstract type.",
                nameof(abstractType));
        }

        if (_possibleTypes.TryGetValue(abstractType.Name, out var possibleTypes))
        {
            return possibleTypes;
        }

        return _possibleTypes.GetOrAdd(
            abstractType.Name,
            static (_, context) => BuildPossibleTypes(context.AbstractType, context.Types),
            new PossibleTypeLookupContext(abstractType, Types));

        static ImmutableArray<FusionObjectTypeDefinition> BuildPossibleTypes(
            ITypeDefinition abstractType,
            FusionTypeDefinitionCollection types)
        {
            if (abstractType is FusionUnionTypeDefinition unionType)
            {
                return [.. unionType.Types.AsEnumerable()];
            }

            if (abstractType is FusionInterfaceTypeDefinition interfaceType)
            {
                var builder = ImmutableArray.CreateBuilder<FusionObjectTypeDefinition>();

                foreach (var type in types)
                {
                    if (type is FusionObjectTypeDefinition obj
                        && obj.Implements.ContainsName(interfaceType.Name))
                    {
                        builder.Add(obj);
                    }
                }

                return builder.ToImmutable();
            }

            if (abstractType is FusionObjectTypeDefinition objectType)
            {
                return [objectType];
            }

            return [];
        }
    }

    IEnumerable<IObjectTypeDefinition> ISchemaDefinition.GetPossibleTypes(
        ITypeDefinition abstractType)
        => GetPossibleTypes(abstractType);

    internal ImmutableArray<Lookup> GetPossibleLookups(
        ITypeDefinition type,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _possibleLookups.GetOrAdd(
            (type.Name, schemaName),
            static (_, c) => c.Schema.GetPossibleLookupsInternal(c.Type, c.SchemaName),
            (Schema: this, Type: type, SchemaName: schemaName));
    }

    private ImmutableArray<Lookup> GetPossibleLookupsInternal(ITypeDefinition type, string? schemaName)
    {
        if (type is FusionComplexTypeDefinition complexType)
        {
            FusionUnionTypeDefinition[]? unionTypes = null;
            FusionObjectTypeDefinition? objectType = null;

            if (complexType.Kind == TypeKind.Object)
            {
                // if we are trying to resolve possible lookups for object types
                // we need to consider lookups for union types where this object type
                // is a member type of.
                //
                // we do not care about the allocation here as we cache the outcome of this method.
                unionTypes = _unionTypes.Where(unionType => unionType.Types.ContainsName(type.Name)).ToArray();
                objectType = (FusionObjectTypeDefinition)complexType;
            }

            var lookups = ImmutableArray.CreateBuilder<Lookup>();

            foreach (var source in complexType.Sources)
            {
                // if the schemaName is null we are considering all possible source schemas.
                if (schemaName is not null
                    && !source.SchemaName.Equals(schemaName, StringComparison.Ordinal))
                {
                    continue;
                }

                lookups.AddRange(source.Lookups);

                foreach (var interfaceType in complexType.Implements)
                {
                    // we will only consider interfaces that are implemented by the complex type
                    // on the current source schema.
                    if (source.Implements.Contains(interfaceType.Name)
                        && interfaceType.Sources.TryGetMember(source.SchemaName, out var interfaceSource))
                    {
                        lookups.AddRange(interfaceSource.Lookups);
                    }
                }

                // we only look at union types if the complex type is an object type.
                if (objectType is not null && unionTypes is not null)
                {
                    var sourceObjectType = (SourceObjectType)source;

                    foreach (var unionType in unionTypes)
                    {
                        // we will only consider unions where the current object type is a member of
                        // on the current source schema.
                        if (sourceObjectType.MemberOf.Contains(unionType.Name)
                            && unionType.Sources.TryGetMember(source.SchemaName, out var unionSource))
                        {
                            lookups.AddRange(unionSource.Lookups);
                        }
                    }
                }
            }

            return lookups.ToImmutable();
        }

        return [];
    }

    public IEnumerable<INameProvider> GetAllDefinitions()
    {
        foreach (var type in Types.AsEnumerable())
        {
            yield return type;
        }

        foreach (var directiveDefinition in DirectiveDefinitions.AsEnumerable())
        {
            yield return directiveDefinition;
        }
    }

    internal void Seal()
    {
        if (_sealed)
        {
            return;
        }

        _sealed = true;
        _features = _features.ToReadOnly();
        _unionTypes = [.. Types.AsEnumerable().OfType<FusionUnionTypeDefinition>()];
    }

    public override string ToString()
        => SchemaFormatter.FormatAsString(this);

    public DocumentNode ToSyntaxNode()
        => SchemaFormatter.FormatAsDocument(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaFormatter.FormatAsDocument(this);

    private record PossibleTypeLookupContext(
        ITypeDefinition AbstractType,
        FusionTypeDefinitionCollection Types);
}
