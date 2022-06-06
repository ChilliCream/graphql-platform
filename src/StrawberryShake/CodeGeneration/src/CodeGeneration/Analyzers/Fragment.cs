using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal class Fragment
{
    public Fragment(
        string name,
        FragmentKind kind,
        INamedType typeCondition,
        SelectionSetNode selectionSet)
    {
        Name = name;
        Kind = kind;
        TypeCondition = typeCondition;
        SelectionSet = selectionSet;
    }

    public string Name { get; }

    public FragmentKind Kind { get; }

    public INamedType TypeCondition { get; }

    public SelectionSetNode SelectionSet { get; }
}
