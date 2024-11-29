using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public class OutputFieldDefinitionBase : FieldDefinitionBase, ICanBeDeprecated
{
    private List<ArgumentDefinition>? _arguments;

    public IList<ArgumentDefinition> Arguments
        => _arguments ??= [];

    /// <summary>
    /// Specifies if this field has any arguments.
    /// </summary>
    public bool HasArguments => _arguments?.Count > 0;

    public IReadOnlyList<ArgumentDefinition> GetArguments()
    {
        if (_arguments is null)
        {
            return Array.Empty<ArgumentDefinition>();
        }

        return _arguments;
    }

    protected void CopyTo(OutputFieldDefinitionBase target)
    {
        base.CopyTo(target);

        if (_arguments?.Count > 0)
        {
            target._arguments = [];

            foreach (var argument in _arguments)
            {
                var newArgument = new ArgumentDefinition();
                argument.CopyTo(newArgument);
                target._arguments.Add(newArgument);
            }
        }

        target.DeprecationReason = DeprecationReason;
    }

    protected void MergeInto(OutputFieldDefinitionBase target)
    {
        base.MergeInto(target);

        if (_arguments is { Count: > 0, })
        {
            target._arguments ??= [];

            foreach (var argument in _arguments)
            {
                var targetArgument =
                    target._arguments.Find(t => t.Name.EqualsOrdinal(argument.Name));

                if (targetArgument is null)
                {
                    targetArgument = new ArgumentDefinition();
                    argument.CopyTo(targetArgument);
                    target._arguments.Add(targetArgument);
                }
                else
                {
                    argument.MergeInto(targetArgument);
                }
            }
        }

        if (DeprecationReason is not null)
        {
            target.DeprecationReason = DeprecationReason;
        }
    }
}
