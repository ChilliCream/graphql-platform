using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using static HotChocolate.Fusion.Types.ThrowHelper;

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
        DeprecationReason = deprecationReason;
        Arguments = arguments;

        // these properties are initialized
        // in the type complete step.
        Type = null!;
        Sources = null!;
        DeclaringType = null!;
    }

    public string Name { get; }

    public string? Description { get; }

    public bool IsDeprecated { get; }

    public string? DeprecationReason { get; }

    public DirectiveCollection Directives
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

    public IType Type
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    public SourceObjectFieldCollection Sources
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    public FusionComplexType DeclaringType
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
        _completed = true;
    }

    public override string ToString()
        => ToSyntaxNode().ToString(indented: true);

    public ISyntaxNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);
}
