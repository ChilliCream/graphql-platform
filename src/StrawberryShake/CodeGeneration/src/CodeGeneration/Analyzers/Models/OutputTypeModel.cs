using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

public sealed class OutputTypeModel : ITypeModel
{
    private static readonly IReadOnlyDictionary<string, DeferredFragmentModel> _empty =
        new Dictionary<string, DeferredFragmentModel>();

    public OutputTypeModel(
        NameString name,
        string? description,
        OutputModelKind kind,
        INamedType type,
        SelectionSetNode selectionSet,
        IReadOnlyList<OutputFieldModel> fields,
        IReadOnlyList<OutputTypeModel>? implements = null,
        IReadOnlyDictionary<string, DeferredFragmentModel>? deferred = null)
    {
        Name = name.EnsureNotEmpty(nameof(name));
        Description = description;
        Kind = kind;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        SelectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        Implements = implements ?? Array.Empty<OutputTypeModel>();
        Deferred = deferred ?? _empty;
    }

    public NameString Name { get; }

    public string? Description { get; }

    public OutputModelKind Kind { get; }

    public bool IsInterface => (Kind & OutputModelKind.Interface) == OutputModelKind.Interface;

    public bool IsFragment => (Kind & OutputModelKind.Fragment) == OutputModelKind.Fragment;

    public INamedType Type { get; }

    public SelectionSetNode SelectionSet { get; }

    public IReadOnlyDictionary<string, DeferredFragmentModel> Deferred { get; }

    public IReadOnlyList<OutputTypeModel> Implements { get; }

    public IReadOnlyList<OutputFieldModel> Fields { get; }

    public override string ToString() => Name;
}

[Flags]
public enum OutputModelKind
{
    Object = 0,
    Interface = 1,
    Fragment = 2,
    FragmentInterface = Fragment | Interface,
    FragmentObject = Fragment | Object
}
