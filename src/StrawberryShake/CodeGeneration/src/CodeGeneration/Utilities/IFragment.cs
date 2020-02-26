using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal interface IFragment
    {
        string Name { get; }

        INamedType TypeCondition { get; }

        SelectionSetNode SelectionSet { get; }
    }
}
