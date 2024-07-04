using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class EmptySelectionCollection : ISelectionCollection
{
    private static readonly ISelection[] _empty = Array.Empty<ISelection>();

    public static EmptySelectionCollection Instance { get; } = new();

    public int Count => 0;

    public ISelection this[int index] => throw new IndexOutOfRangeException();

    public ISelectionCollection Select(string fieldName)
        => Instance;

    public ISelectionCollection Select(ReadOnlySpan<string> fieldNames)
        => Instance;

    public ISelectionCollection Select(INamedType typeContext)
        => Instance;

    public bool IsSelected(string fieldName)
        => false;

    public bool IsSelected(string fieldName1, string fieldName2)
        => false;

    public bool IsSelected(string fieldName1, string fieldName2, string fieldName3)
        => false;

    public bool IsSelected(ISet<string> fieldNames)
        => false;

    public IEnumerator<ISelection> GetEnumerator()
        => _empty.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
