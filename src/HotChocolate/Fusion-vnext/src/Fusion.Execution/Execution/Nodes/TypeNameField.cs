using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Introspection;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed class TypeNameField : IOutputFieldDefinition
{
    public TypeNameField(IOutputType nonNullStringType)
    {
        ArgumentNullException.ThrowIfNull(nonNullStringType);
        Type = nonNullStringType;
        var features = new FeatureCollection();
        features.Set(new ResolveFieldValue(
            ctx => ctx.WriteValue(ctx.Selection.DeclaringSelectionSet.DeclaringOperation.RootType.Name)));
        Features = features.ToReadOnly();
    }

    public string Name => IntrospectionFieldNames.TypeName;

    public string? Description => null;

    public IComplexTypeDefinition DeclaringType
        => throw new NotSupportedException("__typename is a function not an actual field belonging to a type.");

    public ITypeSystemMember DeclaringMember
        => DeclaringType;

    public SchemaCoordinate Coordinate
        => throw new NotSupportedException("__typename is a function not an actual field belonging to a type.");

    public bool IsDeprecated => false;

    public string? DeprecationReason => null;

    public IReadOnlyDirectiveCollection Directives => FusionDirectiveCollection.Empty;

    public IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments
        => FusionInputFieldDefinitionCollection.Empty;

    public IOutputType Type { get; }

    IType IFieldDefinition.Type => Type;

    public FieldFlags Flags => FieldFlags.TypeNameIntrospectionField;

    public IFeatureCollection Features { get; }

    public FieldDefinitionNode ToSyntaxNode()
        => new FieldDefinitionNode(
            null,
            new NameNode(IntrospectionFieldNames.TypeName),
            null,
            [],
            new NonNullTypeNode(new NamedTypeNode("String")),
            []);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => ToSyntaxNode();
}
