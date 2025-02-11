using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionInputFieldDefinition : IInputValueDefinition
{
    private bool _completed;

    public FusionInputFieldDefinition(
        string name,
        string? description,
        IValueNode? defaultValue,
        bool isDeprecated,
        string? deprecationReason)
    {
        Name = name;
        Description = description;
        DefaultValue = defaultValue;
        IsDeprecated = isDeprecated;
        DeprecationReason = deprecationReason;

        // these properties are initialized
        // in the type complete step.
        Directives = null!;
        Type = null!;
    }

    public string Name { get; }

    public string? Description { get; }

    public IValueNode? DefaultValue { get; }

    public bool IsDeprecated { get; }

    public string? DeprecationReason { get; }

    public DirectiveCollection Directives
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    public IType Type
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeInputFieldCompletionContext context)
    {
        ThrowHelper.EnsureNotSealed(_completed);
        Directives = context.Directives;
        Type = context.Type;
        _completed = true;
    }

    public override string ToString()
        => ToSyntaxNode().ToString(indented: true);

    public ISyntaxNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);
}
