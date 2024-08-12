using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class Fragment
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

    public Fragment WithName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(
                $"'{nameof(name)}' cannot be null or empty",
                nameof(name));
        }

        return new Fragment(name,Kind, TypeCondition, SelectionSet);
    }
}
