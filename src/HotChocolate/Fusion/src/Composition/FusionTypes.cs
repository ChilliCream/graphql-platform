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

        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Boolean, out var boolean))
        {
            boolean = new ScalarType(SpecScalarTypes.Boolean) { IsSpecScalar = true };
            _fusionGraph.Types.Add(boolean);
        }

        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Int, out var integer))
        {
            integer = new ScalarType(SpecScalarTypes.Int) { IsSpecScalar = true };
            _fusionGraph.Types.Add(integer);
        }

        Selection = RegisterScalarType(names.SelectionScalar);
        SelectionSet = RegisterScalarType(names.SelectionSetScalar);
        TypeName = RegisterScalarType(names.TypeNameScalar);
        Type = RegisterScalarType(names.TypeScalar);
        Uri = RegisterScalarType(names.UriScalar);
        Resolver = RegisterResolverDirectiveType(
            names.ResolverDirective,
            SelectionSet,
            TypeName);
        Variable = RegisterVariableDirectiveType(
            names.VariableDirective,
            TypeName,
            Selection,
            Type);
        Source = RegisterSourceDirectiveType(
            names.SourceDirective,
            TypeName);
        Fusion = RegisterFusionDirectiveType(
            names.FusionDirective,
            TypeName,
            boolean,
            integer);
        HttpClient = RegisterHttpDirectiveType(
            names.HttpDirective,
            TypeName,
            Uri);
    }

    private string Prefix { get; }

    public ScalarType Selection { get; }

    public ScalarType SelectionSet { get; }

    public ScalarType TypeName { get; }

    public ScalarType Type { get; }

    public ScalarType Uri { get; }

    public DirectiveType Resolver { get; }

    public DirectiveType Variable { get; }

    public DirectiveType Source { get; }

    public DirectiveType HttpClient { get; }

    public DirectiveType Fusion { get; }

    private ScalarType RegisterScalarType(string name)
    {
        var scalarType = new ScalarType(name);
        scalarType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.Types.Add(scalarType);
        return scalarType;
    }

    public Directive CreateVariableDirective(
        string subgraphName,
        string variableName,
        FieldNode select,
        ITypeNode type)
        => new Directive(
            Variable,
            new Argument(SubgraphArg, subgraphName),
            new Argument(NameArg, variableName),
            new Argument(SelectArg, select.ToString(false)),
            new Argument(TypeArg, type.ToString(false)));

    public Directive CreateVariableDirective(
        string subgraphName,
        string variableName,
        string argumentName,
        ITypeNode type)
        => new Directive(
            Variable,
            new Argument(SubgraphArg, subgraphName),
            new Argument(NameArg, variableName),
            new Argument(ArgumentArg, argumentName),
            new Argument(TypeArg, type.ToString(false)));

    private DirectiveType RegisterVariableDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType selection,
        ScalarType type)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Arguments.Add(new InputField(NameArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(SelectArg, selection));
        directiveType.Arguments.Add(new InputField(ArgumentArg, typeName));
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(TypeArg, new NonNullType(type)));
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.Locations |= DirectiveLocation.FieldDefinition;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

    public Directive CreateResolverDirective(
        string subgraphName,
        SelectionSetNode select)
        => new Directive(
            Resolver,
            new Argument(SubgraphArg, subgraphName),
            new Argument(SelectArg, select.ToString(false)));

    private DirectiveType RegisterResolverDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType selectionSet)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Arguments.Add(new InputField(SelectArg, new NonNullType(selectionSet)));
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Locations |= DirectiveLocation.Object;
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
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.FieldDefinition;
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(NameArg, typeName));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

    public Directive CreateHttpDirective(string subgraphName, Uri baseAddress)
        => new Directive(
            HttpClient,
            new Argument(SubgraphArg, subgraphName),
            new Argument(BaseAddressArg, baseAddress.ToString()));

    private DirectiveType RegisterHttpDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType uri)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.FieldDefinition;
        directiveType.Arguments.Add(new InputField(SubgraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(BaseAddressArg, uri));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

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
