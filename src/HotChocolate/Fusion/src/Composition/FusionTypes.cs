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
    private readonly FusionTypeNames _names;
    private readonly Schema _fusionGraph;
    private readonly bool _prefixSelf;

    public FusionTypes(Schema fusionGraph, string? prefix = null, bool prefixSelf = false)
    {
        if (fusionGraph is null)
        {
            throw new ArgumentNullException(nameof(fusionGraph));
        }

        _names = FusionTypeNames.Create(prefix, prefixSelf);
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
            boolean = new ScalarType(SpecScalarTypes.Boolean);
            _fusionGraph.Types.Add(boolean);
        }

        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Int, out var integer))
        {
            integer = new ScalarType(SpecScalarTypes.Int);
            _fusionGraph.Types.Add(integer);
        }

        Selection = RegisterScalarType(_names.SelectionScalar);
        SelectionSet = RegisterScalarType(_names.SelectionSetScalar);
        TypeName = RegisterScalarType(_names.TypeNameScalar);
        Type = RegisterScalarType(_names.TypeScalar);
        Resolver = RegisterResolverDirectiveType(
            _names.ResolverDirective,
            SelectionSet,
            TypeName);
        Variable = RegisterVariableDirectiveType(
            _names.VariableDirective,
            TypeName,
            Selection,
            Type);
        Source = RegisterSourceDirectiveType(
            _names.SourceDirective,
            TypeName);
        RegisterFusionDirectiveType(
            _names.FusionDirective,
            TypeName,
            boolean,
            integer);
    }

    private string Prefix { get; }

    public ScalarType Selection { get; }

    public ScalarType SelectionSet { get; }

    public ScalarType TypeName { get; }

    public ScalarType Type { get; }

    public DirectiveType Resolver { get; }

    public DirectiveType Variable { get; }

    public DirectiveType Source { get; }

    private ScalarType RegisterScalarType(string name)
    {
        var scalarType = new ScalarType(name);
        scalarType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.Types.Add(scalarType);
        return scalarType;
    }

    public Directive CreateVariableDirective(
        string subGraphName,
        string variableName,
        FieldNode select,
        ITypeNode type)
        => new Directive(
            Variable,
            new Argument(SubGraphArg, subGraphName),
            new Argument(NameArg, variableName),
            new Argument(SelectArg, select.ToString(false)),
            new Argument(TypeArg, type.ToString(false)));

    private DirectiveType RegisterVariableDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType selection,
        ScalarType type)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Arguments.Add(new InputField(NameArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(SelectArg, new NonNullType(selection)));
        directiveType.Arguments.Add(new InputField(SubGraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(TypeArg, new NonNullType(type)));
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.Locations |= DirectiveLocation.FieldDefinition;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

    public Directive CreateResolverDirective(
        string subGraphName,
        SelectionSetNode select)
        => new Directive(
            Resolver,
            new Argument(SubGraphArg, subGraphName),
            new Argument(SelectArg, select.ToString(false)));

    private DirectiveType RegisterResolverDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType selectionSet)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Arguments.Add(new InputField(SelectArg, new NonNullType(selectionSet)));
        directiveType.Arguments.Add(new InputField(SubGraphArg, new NonNullType(typeName)));
        directiveType.Locations |= DirectiveLocation.Object;
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

    public Directive CreateSourceDirective(string subGraphName, string? originalName = null)
        => originalName is null
            ? new Directive(
                Source,
                new Argument(SubGraphArg, subGraphName))
            : new Directive(
                Source,
                new Argument(SubGraphArg, subGraphName),
                new Argument(NameArg, originalName));

    private DirectiveType RegisterSourceDirectiveType(string name, ScalarType typeName)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.FieldDefinition;
        directiveType.Arguments.Add(new InputField(SubGraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(NameArg, typeName));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

    private DirectiveType RegisterHttpDirectiveType(
        string name,
        ScalarType typeName,
        ScalarType uri)
    {
        var directiveType = new DirectiveType(name);
        directiveType.Locations = DirectiveLocation.FieldDefinition;
        directiveType.Arguments.Add(new InputField(SubGraphArg, new NonNullType(typeName)));
        directiveType.Arguments.Add(new InputField(BaseAddressArg, typeName));
        directiveType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        _fusionGraph.DirectiveTypes.Add(directiveType);
        return directiveType;
    }

    private void RegisterFusionDirectiveType(
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
    }
}
