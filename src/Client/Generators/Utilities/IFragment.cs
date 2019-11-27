using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Utilities
{
    internal interface IFragment
    {
        string Name { get; }

        INamedType TypeCondition { get; }

        SelectionSetNode SelectionSet { get; }
    }
}
