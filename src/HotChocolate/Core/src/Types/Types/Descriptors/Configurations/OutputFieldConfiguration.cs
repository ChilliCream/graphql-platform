using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Configurations;

public class OutputFieldConfiguration : FieldConfiguration, IDeprecationConfiguration
{
    private List<ArgumentConfiguration>? _arguments;

    /// <summary>
    /// The result type of the resolver.
    /// </summary>
    public Type? ResultType { get; set; }

    public IList<ArgumentConfiguration> Arguments
        => _arguments ??= [];

    /// <summary>
    /// Specifies if this field has any arguments.
    /// </summary>
    public bool HasArguments => _arguments?.Count > 0;

    public IReadOnlyList<ArgumentConfiguration> GetArguments()
    {
        if (_arguments is null)
        {
            return [];
        }

        return _arguments;
    }

    protected void CopyTo(OutputFieldConfiguration target)
    {
        base.CopyTo(target);

        target.ResultType = ResultType;

        if (_arguments?.Count > 0)
        {
            target._arguments = [];

            foreach (var argument in _arguments)
            {
                var newArgument = new ArgumentConfiguration();
                argument.CopyTo(newArgument);
                target._arguments.Add(newArgument);
            }
        }

        target.DeprecationReason = DeprecationReason;
    }

    protected void MergeInto(OutputFieldConfiguration target)
    {
        base.MergeInto(target);

        if (ResultType is not null)
        {
            target.ResultType = ResultType;
        }

        if (_arguments is { Count: > 0 })
        {
            target._arguments ??= [];

            foreach (var argument in _arguments)
            {
                var targetArgument =
                    target._arguments.Find(t => t.Name.EqualsOrdinal(argument.Name));

                if (targetArgument is null)
                {
                    targetArgument = new ArgumentConfiguration();
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
