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
    private readonly Schema _fusionGraph;
    private readonly bool _prefixSelf;

    public FusionTypes(Schema fusionGraph, string? prefix = null, bool prefixSelf = false)
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

        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Boolean, out var booleanType))
        {
            booleanType = new ScalarType(SpecScalarTypes.Boolean) { IsSpecScalar = true, };
            _fusionGraph.Types.Add(booleanType);
        }

        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Int, out var intType))
        {
            intType = new ScalarType(SpecScalarTypes.Int) { IsSpecScalar = true, };
            _fusionGraph.Types.Add(intType);
        }
        
        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.String, out var stringType))
        {
            stringType = new ScalarType(SpecScalarTypes.String) { IsSpecScalar = true, };
            _fusionGraph.Types.Add(stringType);
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

    public ScalarType Selection { get; }

    public ScalarType SelectionSet { get; }

    public ScalarType TypeName { get; }

    public ScalarType Type { get; }

    public ScalarType Uri { get; }

    public InputObjectType ArgumentDefinition { get; }

    public EnumType ResolverKind { get; }

    public DirectiveType Resolver { get; }

    public DirectiveType Variable { get; }

    public DirectiveType Source { get; }

    public DirectiveType Node { get; }

    public DirectiveType ReEncodeId { get; }

    public DirectiveType Transport { get; }

    public DirectiveType Fusion { get; }

    private ScalarType RegisterScalarType(string name)
    {
        var scalarType = new ScalarType(name);
        scalarType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.Types.Add(scalarType);
        return scalarType;
    }

    private InputObjectType RegisterArgumentDefType(
        string name,
        ScalarType typeName,
        ScalarType type)
    {
        var argumentDef = new InputObjectType(name);
        argumentDef.Fields.Add(new InputField(NameArg, new NonNullType(typeName)));
        argumentDef.Fields.Add(new InputField(TypeArg, new NonNullType(type)));
        argumentDef.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.Types.Add(argumentDef);
        return argumentDef;
    }

    private EnumType RegisterResolverKindType(string name)
    {
        var resolverKind = new EnumType(name);
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Fetch));
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Batch));
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Subscribe));
        resolverKind.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.Types.Add(resolverKind);
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

    private DirectiveType RegisterVariableDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType selection)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Arguments.Add(new InputField(NameArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(SelectArg, selection));
        directiveType.Arguments.Add(new InputField(ArgumentArg, typeName));
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.Locations |= DirectiveLocation.FieldDefinition;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

    public Directive CreateReEncodeIdDirective()
        => new Directive(ReEncodeId);

    private DirectiveType RegisterReEncodeIdDirectiveType(string name)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations |= DirectiveLocation.FieldDefinition;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
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
            new(SubgraphArg, subgraphName), new(SelectArg, select.ToString(false))
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
                _ => throw new NotSupportedException()
            };

            directiveArgs.Add(new Argument(KindArg, kindValue));
        }

        return new Directive(Resolver, directiveArgs);
    }

    private DirectiveType RegisterResolverDirectiveType(
        string name,
        ScalarType typeName,
        InputObjectType argumentDef,
        ScalarType selectionSet,
        EnumType resolverKind)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.Arguments.Add(new InputField(SelectArg, new NonNullType(selectionSet)));
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(ArgumentsArg, new ListType(new NonNullType(argumentDef))));
        directiveType.Arguments.Add(new InputField(KindArg, resolverKind));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
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

    private DirectiveType RegisterSourceDirectiveType(string name, ScalarType typeName)
    {
        var directiveType = new DirectiveType(name)
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
                new InputField(SubgraphArg, new NonNullType(typeName)),
                new InputField(NameArg, typeName)
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true
            }
        };
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

    public Directive CreateNodeDirective(string subgraphName, IReadOnlyCollection<ObjectType> types)
    {
        var temp = types.Select(t => new StringValueNode(t.Name)).ToArray();

        return new Directive(
            Node,
            new Argument(SubgraphArg, subgraphName),
            new Argument(TypesArg, new ListValueNode(null, temp)));
    }

    private DirectiveType RegisterNodeDirectiveType(string name, ScalarType typeName)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.Schema;
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(
            new InputField(TypesArg, new NonNullType(new ListType(new NonNullType(typeName)))));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
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

    private DirectiveType RegisterTransportDirectiveType(
        string name,
        ScalarType stringType,
        ScalarType typeName,
        ScalarType uri)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.FieldDefinition;
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(ClientGroupArg, typeName));
        directiveType.Arguments.Add(new InputField(LocationArg, uri));
        directiveType.Arguments.Add(new InputField(KindArg, new NonNullType(stringType)));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
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

    private DirectiveType RegisterFusionDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType boolean,
        ScalarType integer)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.Schema;
        directiveType.Arguments.Add(new InputField(PrefixArg, typeName));
        directiveType.Arguments.Add(new InputField(PrefixSelfArg, boolean));
        directiveType.Arguments.Add(new InputField(VersionArg, integer));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);

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