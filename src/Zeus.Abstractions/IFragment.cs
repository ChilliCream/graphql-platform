using System.Collections.Generic;

namespace Zeus.Abstractions
{
    public interface IFragment
    {
        NamedType TypeCondition { get; }

        IReadOnlyDictionary<string, Directive> Directives { get; }

        ISelectionSet SelectionSet { get; }
    }
}