using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionSchemaDefinition : ISchemaDefinition, IAsyncDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly ConcurrentDictionary<string, ImmutableArray<FusionObjectTypeDefinition>> _possibleTypes = new();
    private readonly ConcurrentDictionary<(string, string?), ImmutableArray<Lookup>> _possibleLookups = new();
    private readonly ConcurrentDictionary<TransitionKey, Lookup> _bestDirectLookup = new();
    private readonly IServiceProvider _services;
    private PlannerTopologyCache? _plannerTopologyCache;
    private ImmutableArray<FusionUnionTypeDefinition> _unionTypes;
    private IFeatureCollection _features;
    private bool _sealed;
    private bool _disposed;

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
        _services = services;
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
    public IServiceProvider Services => _services;

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

        if (!_possibleTypes.TryGetValue(abstractType.Name, out var possibleTypes))
        {
            lock (_lock)
            {
                if (!_possibleTypes.TryGetValue(abstractType.Name, out possibleTypes))
                {
                    possibleTypes = BuildPossibleTypes(abstractType, Types);
                    _possibleTypes.TryAdd(abstractType.Name, possibleTypes);
                }
            }
        }

        return possibleTypes;

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
                    if (type is FusionObjectTypeDefinition obj && obj.Implements.ContainsName(interfaceType.Name))
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

        if (!_possibleLookups.TryGetValue((type.Name, schemaName), out var lookups))
        {
            lock (_lock)
            {
                if (!_possibleLookups.TryGetValue((type.Name, schemaName), out lookups))
                {
                    lookups = GetPossibleLookupsInternal(type, schemaName);
                    _possibleLookups.TryAdd((type.Name, schemaName), lookups);
                }
            }
        }

        return lookups;
    }

    internal ImmutableArray<Lookup> GetPossibleLookupsOrdered(
        ITypeDefinition type,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (_plannerTopologyCache is { } topology
            && topology.TryGetOrderedLookups(type.Name, schemaName, out var lookups))
        {
            return lookups;
        }

        return [.. GetPossibleLookups(type, schemaName).OrderBy(CreateLookupOrderingKey, StringComparer.Ordinal)];
    }

    internal bool TryGetFieldResolution(
        FusionComplexTypeDefinition type,
        string fieldName,
        out FieldResolutionInfo fieldResolution)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        if (_plannerTopologyCache is { } topology
            && topology.TryGetFieldResolution(type.Name, fieldName, out fieldResolution))
        {
            return true;
        }

        if (type.Fields.TryGetField(fieldName, allowInaccessibleFields: true, out var field))
        {
            fieldResolution = new FieldResolutionInfo(
                field.Sources.Schemas.OrderBy(static s => s, StringComparer.Ordinal).ToImmutableArray(),
                field.Sources.Members
                    .Where(static s => s.Requirements is not null)
                    .Select(static s => s.SchemaName)
                    .OrderBy(static s => s, StringComparer.Ordinal)
                    .ToImmutableArray());
            return true;
        }

        fieldResolution = default;
        return false;
    }

    internal bool TryGetTypeScatter(
        FusionComplexTypeDefinition type,
        out TypeScatterInfo typeScatter)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (_plannerTopologyCache is { } topology
            && topology.TryGetTypeScatter(type.Name, out typeScatter))
        {
            return true;
        }

        typeScatter = default;
        return false;
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
                if (schemaName is not null && !source.SchemaName.Equals(schemaName, StringComparison.Ordinal))
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

    /// <summary>
    /// Tries to get the best direct lookup to transition from one schema to another without intermediary transitions.
    /// The best lookup algorithm will try to find the smallest possible key that does not require any intermediary transitions.
    /// </summary>
    /// <param name="type">The type to get the best direct lookup for.</param>
    /// <param name="fromSchemas">The schemas to get the best direct lookup from.</param>
    /// <param name="toSchema">The schema to get the best direct lookup to.</param>
    /// <param name="lookup">The best direct lookup.</param>
    /// <returns>True if the best direct lookup was found, false otherwise.</returns>
    public bool TryGetBestDirectLookup(
        FusionComplexTypeDefinition type,
        ImmutableHashSet<string> fromSchemas,
        string toSchema,
        [NotNullWhen(true)] out Lookup? lookup)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(fromSchemas);
        ArgumentException.ThrowIfNullOrEmpty(toSchema);

        foreach (var fromSchema in fromSchemas)
        {
            if (TryGetBestDirectLookup(type, fromSchema, toSchema, out lookup))
            {
                return true;
            }
        }

        lookup = null;
        return false;
    }

    /// <summary>
    /// Tries to get the best direct lookup to transition from one schema to another without intermediary transitions.
    /// The best lookup algorithm will try to find the smallest possible key that does not require
    /// any intermediary transitions.
    /// </summary>
    /// <param name="type">The type to get the best direct lookup for.</param>
    /// <param name="fromSchema">The schema to get the best direct lookup from.</param>
    /// <param name="toSchema">The schema to get the best direct lookup to.</param>
    /// <param name="lookup">The best direct lookup.</param>
    /// <returns>True if the best direct lookup was found, false otherwise.</returns>
    public bool TryGetBestDirectLookup(
        FusionComplexTypeDefinition type,
        string fromSchema,
        string toSchema,
        [NotNullWhen(true)] out Lookup? lookup)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(fromSchema);
        ArgumentException.ThrowIfNullOrEmpty(toSchema);

        if (_plannerTopologyCache is { } topology)
        {
            if (topology.TryGetDirectTransition(type.Name, fromSchema, toSchema, out lookup))
            {
                return true;
            }

            if (topology.IsDirectTransitionImpossible(type.Name, fromSchema, toSchema))
            {
                lookup = null;
                return false;
            }
        }

        if (!_bestDirectLookup.TryGetValue(new TransitionKey(type.Name, fromSchema, toSchema), out lookup))
        {
            var keyTransitionVisitor = new KeyTransitionVisitor();

            var context = new KeyTransitionVisitor.Context
            {
                CompositeSchema = this,
                SourceSchema = fromSchema,
                Types = [type]
            };

            Lookup? bestLookup = null;
            var fields = 0;
            var fragments = 0;

            foreach (var possibleLookup in GetPossibleLookups(type, toSchema))
            {
                context.Reset();
                keyTransitionVisitor.Visit(possibleLookup.Requirements, context);

                if (context.NeedsTransition)
                {
                    continue;
                }

                if (context is { Fields: 1, Fragments: 0 })
                {
                    bestLookup = possibleLookup;
                    break;
                }

                if (bestLookup is null)
                {
                    bestLookup = possibleLookup;
                    fields = context.Fields;
                    fragments = context.Fragments;
                    continue;
                }

                if (context.Fields < fields)
                {
                    bestLookup = possibleLookup;
                    fields = context.Fields;
                    fragments = context.Fragments;
                }

                if (context.Fields == fields && context.Fragments < fragments)
                {
                    bestLookup = possibleLookup;
                    fields = context.Fields;
                    fragments = context.Fragments;
                }
            }

            if (bestLookup is not null)
            {
                _bestDirectLookup.TryAdd(new TransitionKey(type.Name, fromSchema, toSchema), bestLookup);
            }

            lookup = bestLookup;
        }

        return lookup is not null;
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

    internal void InitializePlannerTopologyCache()
    {
        _plannerTopologyCache ??= PlannerTopologyCache.Build(this);
    }

    public override string ToString()
        => SchemaFormatter.FormatAsString(this);

    public DocumentNode ToSyntaxNode()
        => SchemaFormatter.FormatAsDocument(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaFormatter.FormatAsDocument(this);

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (Features.TryGet(out SchemaCancellationFeature? cancellation))
            {
                await cancellation.DisposeAsync().ConfigureAwait(false);
            }

            if (_services is IAsyncDisposable disposableServices)
            {
                await disposableServices.DisposeAsync().ConfigureAwait(false);
            }

            _disposed = true;
        }
    }

    private readonly record struct TransitionKey(string TypeName, string From, string To);

    private static string CreateLookupOrderingKey(Lookup lookup)
    {
        var path = lookup.Path.Length == 0
            ? string.Empty
            : string.Join('.', lookup.Path);

        return string.Concat(
            lookup.SchemaName,
            ":",
            lookup.FieldName,
            ":",
            path,
            ":",
            lookup.Arguments.Length.ToString(),
            ":",
            lookup.Fields.Length.ToString());
    }
}

internal sealed class KeyTransitionVisitor : SyntaxWalker<KeyTransitionVisitor.Context>
{
    protected override ISyntaxVisitorAction Enter(FieldNode node, Context context)
    {
        var type = (FusionComplexTypeDefinition)context.Types.Peek();
        var field = type.Fields.GetField(node.Name.Value, allowInaccessibleFields: true);

        if (!field.Sources.TryGetMember(context.SourceSchema, out var member) || member.Requirements is not null)
        {
            context.NeedsTransition = true;
            return Break;
        }

        context.Fields++;
        context.Types.Push(field.Type.NamedType());
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(FieldNode node, Context context)
    {
        context.Types.Pop();
        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, Context context)
    {
        context.Fragments++;

        if (node.TypeCondition is { Name: { } typeName })
        {
            context.Types.Push(context.CompositeSchema.Types[typeName.Value]);
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(InlineFragmentNode node, Context context)
    {
        if (node.TypeCondition is not null)
        {
            context.Types.Pop();
        }

        return base.Leave(node, context);
    }

    public sealed class Context
    {
        public required FusionSchemaDefinition CompositeSchema { get; init; }

        public required string SourceSchema { get; init; }

        public required List<ITypeDefinition> Types { get; init; }

        public bool NeedsTransition { get; set; }

        public int Fields { get; set; }

        public int Fragments { get; set; }

        public void Reset()
        {
            var first = Types[0];
            Types.Clear();
            NeedsTransition = false;
            Fields = 0;
            Fragments = 0;
            Types.Push(first);
        }
    }
}
