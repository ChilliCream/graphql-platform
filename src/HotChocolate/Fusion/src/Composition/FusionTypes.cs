using HotChocolate.Fusion.Composition.Properties;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Registers and provides access to internal fusion types.
/// </summary>
public sealed class FusionTypes : IFusionTypeContext
{
    private readonly Schema _fusionGraph;
    private readonly Dictionary<string, INamedType> _types = new();

    public FusionTypes(Schema fusionGraph, string? prefix = null, bool prefixSelf = false)
    {
        if (fusionGraph is null)
        {
            throw new ArgumentNullException(nameof(fusionGraph));
        }

        var names = FusionTypeNames.Create(prefix, prefixSelf);
        _fusionGraph = fusionGraph;

        Prefix = prefix ?? string.Empty;

        if (_fusionGraph.ContextData.TryGetValue(nameof(FusionTypes), out var value) &&
            (value is not string prefixValue || !Prefix.EqualsOrdinal(prefixValue)))
        {
            throw new ArgumentException(
                CompositionResources.FusionTypes_EnsureInitialized_Failed,
                nameof(fusionGraph));
        }

        RewriteType(CreateScalarType(FusionTypeBaseNames.Uri), names.UriScalar);
        RewriteType(CreateScalarType(FusionTypeBaseNames.Name), names.NameScalar);
        RewriteType(CreateScalarType(FusionTypeBaseNames.SchemaCoordinate), names.SchemaCoordinateScalar);
        RewriteType(CreateScalarType(FusionTypeBaseNames.Selection), names.SelectionScalar);
        RewriteType(CreateScalarType(FusionTypeBaseNames.SelectionSet), names.SelectionSetScalar);
        RewriteType(CreateScalarType(FusionTypeBaseNames.OperationDefinition), names.OperationDefinitionScalar);
        RewriteType(CreateResolverKindType(), names.ResolverKindEnum);
        CreateSpecScalars();
        
        DeclareDirective = RewriteType(Composition.DeclareDirective.CreateType(), names.DeclareDirective);
        FusionDirective = RewriteType(Composition.FusionDirective.CreateType(), names.FusionDirective);
        IsDirective = RewriteType(Composition.IsDirective.CreateType(), names.IsDirective);
        NodeDirective = RewriteType(Composition.NodeDirective.CreateType(), names.NodeDirective);
        PrivateDirective = RewriteType(Composition.PrivateDirective.CreateType(), names.PrivateDirective);
        RemoveDirective = RewriteType(Composition.RemoveDirective.CreateType(), names.RemoveDirective);
        RenameDirective = RewriteType(Composition.RenameDirective.CreateType(), names.RenameDirective);
        RequireDirective = RewriteType(Composition.RequireDirective.CreateType(), names.RequireDirective);
        ResolveDirective = RewriteType(Composition.ResolveDirective.CreateType(), names.ResolveDirective);
        ResolverDirective = RewriteType(Composition.ResolverDirective.CreateType(), names.ResolverDirective);
        SourceDirective = RewriteType(Composition.SourceDirective.CreateType(), names.SourceDirective);
        TransportDirective = RewriteType(Composition.TransportDirective.CreateType(), names.TransportDirective);
        VariableDirective = RewriteType(Composition.VariableDirective.CreateType(), names.VariableDirective);
    }

    private string Prefix { get; }

    public DirectiveType DeclareDirective { get; }

    public DirectiveType FusionDirective { get; }

    public DirectiveType IsDirective { get; }

    public DirectiveType NodeDirective { get; }

    public DirectiveType PrivateDirective { get; }

    public DirectiveType RemoveDirective { get; }

    public DirectiveType RequireDirective { get; }

    public DirectiveType RenameDirective { get; }

    public DirectiveType ResolveDirective { get; }

    public DirectiveType ResolverDirective { get; }

    public DirectiveType SourceDirective { get; }

    public DirectiveType TransportDirective { get; }

    public DirectiveType VariableDirective { get; }

    private static EnumType CreateResolverKindType()
    {
        var resolverKind = new EnumType(FusionTypeBaseNames.ResolverKind);
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Fetch));
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Batch));
        resolverKind.Values.Add(new EnumValue(FusionEnumValueNames.Subscribe));
        resolverKind.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return resolverKind;
    }

    private void CreateSpecScalars()
    {
        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Boolean, out var booleanType))
        {
            booleanType = new ScalarType(SpecScalarTypes.Boolean) { IsSpecScalar = true };
            _fusionGraph.Types.Add(booleanType);
        }
        _types.Add(booleanType.Name, booleanType);

        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.Int, out var intType))
        {
            intType = new ScalarType(SpecScalarTypes.Int) { IsSpecScalar = true };
            _fusionGraph.Types.Add(intType);
        }
        _types.Add(intType.Name, intType);
        
        if (!_fusionGraph.Types.TryGetType<ScalarType>(SpecScalarTypes.String, out var stringType))
        {
            stringType = new ScalarType(SpecScalarTypes.String) { IsSpecScalar = true };
            _fusionGraph.Types.Add(stringType);
        }
        _types.Add(stringType.Name, stringType);
    }
    
    private static ScalarType CreateScalarType(string name)
    {
        var scalarType = new ScalarType(name);
        scalarType.ContextData.Add(WellKnownContextData.IsFusionType, true);
        return scalarType;
    }

    private T RewriteType<T>(T member, string name) where T : ITypeSystemMember 
    {
        switch (member)
        {
            case DirectiveType directiveType:
                directiveType.Name = name;

                foreach (var argument in directiveType.Arguments)
                {
                    argument.Type = argument.Type.ReplaceNameType(n => _types[n]);
                }

                return member;
            
            case EnumType enumType:
                _types.Add(enumType.Name, enumType);
                enumType.Name = name;
                return member;
            
            case ScalarType scalarType:
                _types.Add(scalarType.Name, scalarType);
                scalarType.Name = name;
                return member;
            
            default:
                throw new NotSupportedException();
        }
    }
}