using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionEnumValue : IEnumValue
{
    private bool _completed;

    public FusionEnumValue(
        string name,
        string? description,
        bool isDeprecated,
        string? deprecationReason)
    {
        Name = name;
        Description = description;
        IsDeprecated = isDeprecated;
        DeprecationReason = deprecationReason;

        // these properties are initialized
        // in the type complete step.
        DeclaringType = null!;
        Directives = null!;
        Features = null!;
    }

    public string Name { get; }

    public string? Description { get; }

    public IEnumTypeDefinition DeclaringType
    {
        get;
        set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    public SchemaCoordinate Coordinate => new(DeclaringType.Name, Name, ofDirective: false);

    public bool IsDeprecated { get; }

    public string? DeprecationReason { get; }

    public FusionDirectiveCollection Directives
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    public IFeatureCollection Features
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeEnumValueCompletionContext context)
    {
        ThrowHelper.EnsureNotSealed(_completed);
        DeclaringType = context.DeclaringType;
        Directives = context.Directives;
        Features = context.Features;
        _completed = true;
    }

    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(indented: true);

    public EnumValueDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);
}
