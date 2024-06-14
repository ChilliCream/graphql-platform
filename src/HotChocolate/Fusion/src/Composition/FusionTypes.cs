using HotChocolate.Fusion.Composition.Properties;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Registers and provides access to internal fusion types.
/// </summary>
public sealed class FusionTypes
{
    private readonly SchemaDefinition _fusionGraph;
    private readonly bool _prefixSelf;

    public FusionTypes(SchemaDefinition fusionGraph, string? prefix = null, bool prefixSelf = false)
    {
        if (fusionGraph is null)
        {
            throw new ArgumentNullException(nameof(fusionGraph));
        }

        var names = FusionTypeNames.Create(prefix, prefixSelf);
        _fusionGraph = fusionGraph;
        _prefixSelf = prefixSelf;

        Prefix = prefix ?? string.Empty;

        if (_fusionGraph.ContextData.TryGetValue(nameof(FusionTypes), out var value) &&
            (value is not string prefixValue || !Prefix.EqualsOrdinal(prefixValue)))
        {
            throw new ArgumentException(
                CompositionResources.FusionTypes_EnsureInitialized_Failed,
                nameof(fusionGraph));
        }

        if (!_fusionGraph.TypeDefinitions.TryGetType<ScalarTypeDefinition>(SpecScalarTypes.Boolean, out var booleanType))
        {
            booleanType = new ScalarTypeDefinition(SpecScalarTypes.Boolean) { IsSpecScalar = true, };
            _fusionGraph.TypeDefinitions.Add(booleanType);
        }

        if (!_fusionGraph.TypeDefinitions.TryGetType<ScalarTypeDefinition>(SpecScalarTypes.Int, out var intType))
        {
            intType = new ScalarTypeDefinition(SpecScalarTypes.Int) { IsSpecScalar = true, };
            _fusionGraph.TypeDefinitions.Add(intType);
        }

        if (!_fusionGraph.TypeDefinitions.TryGetType<ScalarTypeDefinition>(SpecScalarTypes.String, out var stringType))
        {
            stringType = new ScalarTypeDefinition(SpecScalarTypes.String) { IsSpecScalar = true, };
            _fusionGraph.TypeDefinitions.Add(stringType);
        }

        Selection = RegisterScalarType(names.SelectionScalar);
        SelectionSet = RegisterScalarType(names.SelectionSetScalar);
        TypeName = RegisterScalarType(names.TypeNameScalar);
        Type = RegisterScalarType(names.TypeScalar);
        Uri = RegisterScalarType(names.UriScalar);
        ArgumentDefinition = RegisterArgumentDefType(names.ArgumentDefinition, TypeName, Type);
        ResolverKind = RegisterResolverKindType(names.ResolverKind);
        Resolver = RegisterResolverDirectiveType(
            names.ResolverDirective,
            SelectionSet,
            ArgumentDefinition,
            SelectionSet,
            ResolverKind);
        Variable = RegisterVariableDirectiveType(
            names.VariableDirective,
            TypeName,
            Selection);
        Source = RegisterSourceDirectiveType(
            names.SourceDirective,
            TypeName);
        Node = RegisterNodeDirectiveType(
            names.NodeDirective,
            TypeName);
        ReEncodeId = RegisterReEncodeIdDirectiveType(
            names.ReEncodeIdDirective);
        Fusion = RegisterFusionDirectiveType(
            names.FusionDirective,
            TypeName,
            booleanType,
            intType);
        Transport = RegisterTransportDirectiveType(
            names.TransportDirective,
            stringType,
            TypeName,
            Uri);
    }

    private string Prefix { get; }

    public ScalarTypeDefinition Selection { get; }

    public ScalarTypeDefinition SelectionSet { get; }

    public ScalarTypeDefinition TypeName { get; }

    public ScalarTypeDefinition Type { get; }

    public ScalarTypeDefinition Uri { get; }

    public InputObjectTypeDefinition ArgumentDefinition { get; }

    public EnumTypeDefinition ResolverKind { get; }

    public DirectiveDefinition Resolver { get; }

    public DirectiveDefinition Variable { get; }

    public DirectiveDefinition Source { get; }

    public DirectiveDefinition Node { get; }

    public DirectiveDefinition ReEncodeId { get; }

    public DirectiveDefinition Transport { get; }

    public DirectiveDefinition Fusion { get; }

    private ScalarTypeDefinition RegisterScalarType(string name)
    {
        var scalarType = new ScalarTypeDefinition(name);
        scalarType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.TypeDefinitions.Add(scalarType);
        return scalarType;
    }

    private InputObjectTypeDefinition RegisterArgumentDefType(
        string name,
        ScalarTypeDefinition typeName,
        ScalarTypeDefinition type)
    {
        var argumentDef = new InputObjectTypeDefinition(name);
        argumentDef.Fields.Add(new InputFieldDefinition(NameArg, new NonNullTypeDefinition(typeName)));
        argumentDef.Fields.Add(new InputFieldDefinition(TypeArg, new NonNullTypeDefinition(type)));
        argumentDef.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.TypeDefinitions.Add(argumentDef);
        return argumentDef;
    }

    private EnumTypeDefinition RegisterResolverKindType(string name)
    {
        var resolverKind = new EnumTypeDefinition(name);
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Fetch));
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Batch));
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Subscribe));
        resolverKind.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.TypeDefinitions.Add(resolverKind);
        return resolverKind;
    }

    public Directive CreateVariableDirective(
        string subgraphName,
        string variableName,
        FieldNode select)
        => new Directive(
            Variable,
            new Argument(SubgraphArg, subgraphName),
            new Argument(NameArg, variableName),
            new Argument(SelectArg, select.ToString(false)));

    public Directive CreateVariableDirective(
        string subgraphName,
        string variableName,
        string argumentName)
        => new Directive(
            Variable,
            new Argument(SubgraphArg, subgraphName),
            new Argument(NameArg, variableName),
            new Argument(ArgumentArg, argumentName));

    private DirectiveDefinition RegisterVariableDirectiveType(
        string name,
        ScalarTypeDefinition typeName,
        ScalarTypeDefinition selection)
    {
        var directiveType = new DirectiveDefinition(name);
        directiveType.Arguments.Add(new InputFieldDefinition(NameArg, new NonNullTypeDefinition(typeName)));
        directiveType.Arguments.Add(new InputFieldDefinition(SelectArg, selection));
        directiveType.Arguments.Add(new InputFieldDefinition(ArgumentArg, typeName));
        directiveType.Arguments.Add(new InputFieldDefinition(SubgraphArg, new NonNullTypeDefinition(typeName)));
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.Locations |= DirectiveLocation.FieldDefinition;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveDefinitions.Add(directiveType);
        return directiveType;
    }

    public Directive CreateReEncodeIdDirective()
        => new Directive(ReEncodeId);

    private DirectiveDefinition RegisterReEncodeIdDirectiveType(string name)
    {
        var directiveType = new DirectiveDefinition(name);
        directiveType.Locations |= DirectiveLocation.FieldDefinition;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveDefinitions.Add(directiveType);
        return directiveType;
    }

    public Directive CreateResolverDirective(
        string subgraphName,
        SelectionSetNode select,
        Dictionary<string, ITypeNode>? arguments = null,
        EntityResolverKind kind = EntityResolverKind.Single)
    {
        var directiveArgs = new List<Argument>
        {
            new(SubgraphArg, subgraphName), new(SelectArg, select.ToString(false)),
        };

        if (arguments is { Count: > 0, })
        {
            var argumentDefs = new List<IValueNode>();

            foreach (var argumentDef in arguments)
            {
                argumentDefs.Add(
                    new ObjectValueNode(
                        new ObjectFieldNode(
                            NameArg,
                            argumentDef.Key),
                        new ObjectFieldNode(
                            TypeArg,
                            argumentDef.Value.ToString(false))));
            }

            directiveArgs.Add(new Argument(ArgumentsArg, new ListValueNode(argumentDefs)));
        }

        if (kind != EntityResolverKind.Single)
        {
            var kindValue = kind switch
            {
                EntityResolverKind.Batch => FusionEnumValueNames.Batch,
                EntityResolverKind.Subscribe => FusionEnumValueNames.Subscribe,
                _ => throw new NotSupportedException(),
            };

            directiveArgs.Add(new Argument(KindArg, kindValue));
        }

        return new Directive(Resolver, directiveArgs);
    }

    private DirectiveDefinition RegisterResolverDirectiveType(
        string name,
        ScalarTypeDefinition typeName,
        InputObjectTypeDefinition argumentDef,
        ScalarTypeDefinition selectionSet,
        EnumTypeDefinition resolverKind)
    {
        var directiveType = new DirectiveDefinition(name);
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.Arguments.Add(new InputFieldDefinition(SelectArg, new NonNullTypeDefinition(selectionSet)));
        directiveType.Arguments.Add(new InputFieldDefinition(SubgraphArg, new NonNullTypeDefinition(typeName)));
        directiveType.Arguments.Add(new InputFieldDefinition(ArgumentsArg, new ListTypeDefinition(new NonNullTypeDefinition(argumentDef))));
        directiveType.Arguments.Add(new InputFieldDefinition(KindArg, resolverKind));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveDefinitions.Add(directiveType);
        return directiveType;
    }

    public Directive CreateSourceDirective(string subgraphName, string? originalName = null)
        => originalName is null
            ? new Directive(
                Source,
                new Argument(SubgraphArg, subgraphName))
            : new Directive(
                Source,
                new Argument(SubgraphArg, subgraphName),
                new Argument(NameArg, originalName));

    private DirectiveDefinition RegisterSourceDirectiveType(string name, ScalarTypeDefinition typeName)
    {
        var directiveType = new DirectiveDefinition(name)
        {
            Locations = DirectiveLocation.Object |
                DirectiveLocation.FieldDefinition |
                DirectiveLocation.Enum |
                DirectiveLocation.EnumValue |
                DirectiveLocation.InputObject |
                DirectiveLocation.InputFieldDefinition |
                DirectiveLocation.Scalar,
            Arguments =
            {
                new InputFieldDefinition(SubgraphArg, new NonNullTypeDefinition(typeName)),
                new InputFieldDefinition(NameArg, typeName),
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true,
            },
        };
        _fusionGraph.DirectiveDefinitions.Add(directiveType);
        return directiveType;
    }

    public Directive CreateNodeDirective(string subgraphName, IReadOnlyCollection<ObjectTypeDefinition> types)
    {
        var temp = types.Select(t => new StringValueNode(t.Name)).ToArray();

        return new Directive(
            Node,
            new Argument(SubgraphArg, subgraphName),
            new Argument(TypesArg, new ListValueNode(null, temp)));
    }

    private DirectiveDefinition RegisterNodeDirectiveType(string name, ScalarTypeDefinition typeName)
    {
        var directiveType = new DirectiveDefinition(name);
        directiveType.Locations = DirectiveLocation.Schema;
        directiveType.Arguments.Add(new InputFieldDefinition(SubgraphArg, new NonNullTypeDefinition(typeName)));
        directiveType.Arguments.Add(
            new InputFieldDefinition(TypesArg, new NonNullTypeDefinition(new ListTypeDefinition(new NonNullTypeDefinition(typeName)))));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveDefinitions.Add(directiveType);
        return directiveType;
    }

    public Directive CreateHttpDirective(string subgraphName, string? clientName, Uri location)
        =>  clientName is null
            ? new Directive(
                Transport,
                new Argument(SubgraphArg, subgraphName),
                new Argument(LocationArg, location.ToString()),
                new Argument(KindArg, "HTTP"))
            : new Directive(
                Transport,
                new Argument(SubgraphArg, subgraphName),
                new Argument(ClientGroupArg, clientName),
                new Argument(LocationArg, location.ToString()),
                new Argument(KindArg, "HTTP"));

    private DirectiveDefinition RegisterTransportDirectiveType(
        string name,
        ScalarTypeDefinition stringType,
        ScalarTypeDefinition typeName,
        ScalarTypeDefinition uri)
    {
        var directiveType = new DirectiveDefinition(name);
        directiveType.Locations = DirectiveLocation.FieldDefinition;
        directiveType.Arguments.Add(new InputFieldDefinition(SubgraphArg, new NonNullTypeDefinition(typeName)));
        directiveType.Arguments.Add(new InputFieldDefinition(ClientGroupArg, typeName));
        directiveType.Arguments.Add(new InputFieldDefinition(LocationArg, uri));
        directiveType.Arguments.Add(new InputFieldDefinition(KindArg, new NonNullTypeDefinition(stringType)));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveDefinitions.Add(directiveType);
        return directiveType;
    }

    public Directive CreateWebSocketDirective(string subgraphName, string? clientName, Uri location)
        =>  clientName is null
            ? new Directive(
                Transport,
                new Argument(SubgraphArg, subgraphName),
                new Argument(LocationArg, location.ToString()),
                new Argument(KindArg, "WebSocket"))
            : new Directive(
                Transport,
                new Argument(SubgraphArg, subgraphName),
                new Argument(ClientGroupArg, clientName),
                new Argument(LocationArg, location.ToString()),
                new Argument(KindArg, "WebSocket"));

    private DirectiveDefinition RegisterFusionDirectiveType(
        string name,
        ScalarTypeDefinition typeName,
        ScalarTypeDefinition boolean,
        ScalarTypeDefinition integer)
    {
        var directiveType = new DirectiveDefinition(name);
        directiveType.Locations = DirectiveLocation.Schema;
        directiveType.Arguments.Add(new InputFieldDefinition(PrefixArg, typeName));
        directiveType.Arguments.Add(new InputFieldDefinition(PrefixSelfArg, boolean));
        directiveType.Arguments.Add(new InputFieldDefinition(VersionArg, integer));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveDefinitions.Add(directiveType);

        if (string.IsNullOrEmpty(Prefix))
        {
            _fusionGraph.Directives.Add(
                new Directive(
                    directiveType,
                    new Argument(VersionArg, 1)));
        }
        else
        {
            _fusionGraph.Directives.Add(
                new Directive(
                    directiveType,
                    new Argument(PrefixArg, Prefix),
                    new Argument(PrefixSelfArg, _prefixSelf),
                    new Argument(VersionArg, 1)));
        }

        return directiveType;
    }
}
