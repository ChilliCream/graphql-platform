using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal sealed class FragmentIndexEntry
{
    public FragmentIndexEntry(FragmentDefinitionNode fragment, INamedOutputType typeCondition)
    {
        Fragment = fragment;
        TypeCondition = typeCondition;
    }

    public string Name => Fragment.Name.Value;

    public FragmentDefinitionNode Fragment { get; }

    public INamedOutputType TypeCondition { get; }

    public HashSet<string> Siblings { get; } = new();

    public HashSet<string> DependsOn { get; } = new();

    public Fragment ToFragment()
        => new Fragment(Name, FragmentKind.Named, TypeCondition, Fragment.SelectionSet);
}
