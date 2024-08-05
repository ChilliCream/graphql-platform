using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeObjectField(
    string name,
    string? description,
    bool isDeprecated,
    string? deprecationReason,
    CompositeInputFieldCollection arguments)
    : ICompositeField
{
    private ICompositeType _type = default!;
    private SourceObjectFieldCollection _sources = default!;
    private DirectiveCollection _directives = default!;
    private bool _completed;
    public string Name { get; } = name;

    public string? Description { get; } = description;

    public bool IsDeprecated { get; } = isDeprecated;

    public string? DeprecationReason { get; } = deprecationReason;

    public DirectiveCollection Directives
    {
        get => _directives;
        private set
        {
            if (_completed)
            {
                throw new NotSupportedException(
                    "The type definition is sealed and cannot be modified.");
            }

            _directives = value;
        }
    }

    public CompositeInputFieldCollection Arguments { get; } = arguments;

    public ICompositeType Type
    {
        get => _type;
        private set
        {
            if (_completed)
            {
                throw new NotSupportedException(
                    "The type definition is sealed and cannot be modified.");
            }

            _type = value;
        }
    }

    public SourceObjectFieldCollection Sources
    {
        get => _sources;
        private set
        {
            if (_completed)
            {
                throw new NotSupportedException(
                    "The type definition is sealed and cannot be modified.");
            }

            _sources = value;
        }
    }

    internal void Complete(CompositeObjectFieldCompletionContext context)
    {
        if (_completed)
        {
            throw new NotSupportedException(
                "The type definition is sealed and cannot be modified.");
        }

        Directives = context.Directives;
        Type = context.Type;
        Sources = context.Sources;
        _completed = true;
    }

    public override string ToString() => Name;
}
