using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class Fragment
{
    public Fragment(
        string name,
        FragmentKind kind,
        ITypeDefinition typeCondition,
        SelectionSetNode selectionSet)
    {
        Name = name;
        Kind = kind;
        TypeCondition = typeCondition;
        SelectionSet = selectionSet;
    }

    public string Name { get; }

    public FragmentKind Kind { get; }

    public ITypeDefinition TypeCondition { get; }

    public SelectionSetNode SelectionSet { get; }

    public Fragment WithName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return new Fragment(name, Kind, TypeCondition, SelectionSet);
    }
}
