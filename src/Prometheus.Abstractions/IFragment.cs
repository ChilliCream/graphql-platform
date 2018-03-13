using System.Collections.Generic;

namespace Prometheus.Abstractions
{
    public interface IFragment
    {
        NamedType TypeCondition { get; }

        IReadOnlyDictionary<string, Directive> Directives { get; }

        ISelectionSet SelectionSet { get; }
    }
}