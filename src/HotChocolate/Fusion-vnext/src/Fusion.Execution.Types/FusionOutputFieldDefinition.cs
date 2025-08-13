using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Types.ThrowHelper;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Fusion.Types;

public sealed class FusionOutputFieldDefinition : IOutputFieldDefinition
{
    private bool _completed;

    public FusionOutputFieldDefinition(
        string name,
        string? description,
        bool isDeprecated,
        string? deprecationReason,
        FusionInputFieldDefinitionCollection arguments)
    {
        Name = name;
        Description = description;
        IsDeprecated = isDeprecated;
        IsIntrospectionField = name.StartsWith("__");
        DeprecationReason = deprecationReason;
        Arguments = arguments;

        // these properties are initialized
        // in the type complete step.
        Type = null!;
        Sources = null!;
        DeclaringType = null!;
        Directives = null!;
        Features = null!;
    }

    public string Name { get; }

    public string? Description { get; }

    public FusionComplexTypeDefinition DeclaringType
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    IComplexTypeDefinition IOutputFieldDefinition.DeclaringType => DeclaringType;

    ITypeSystemMember IFieldDefinition.DeclaringMember => DeclaringType;

    public SchemaCoordinate Coordinate => new(DeclaringType.Name, Name, ofDirective: false);

    public bool IsDeprecated { get; }

    public bool IsIntrospectionField { get; }

    public string? DeprecationReason { get; }

    public FusionDirectiveCollection Directives
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => Directives;

    public FusionInputFieldDefinitionCollection Arguments { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IOutputFieldDefinition.Arguments
        => Arguments;

    public IOutputType Type
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    public FieldFlags Flags => FieldFlags.None;

    IType IFieldDefinition.Type => Type;

    public SourceObjectFieldCollection Sources
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    public IFeatureCollection Features
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeObjectFieldCompletionContext context)
    {
        EnsureNotSealed(_completed);
        Directives = context.Directives;
        Type = context.Type;
        Sources = context.Sources;
        DeclaringType = context.DeclaringType;
        Features = context.Features;
        _completed = true;
    }

    public override string ToString()
        => ToSyntaxNode().ToString(indented: true);

    public FieldDefinitionNode ToSyntaxNode()
        => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => Format(this);
}
