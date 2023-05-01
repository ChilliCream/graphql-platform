using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Contains the bindings a type system member has to type system members of other subgraphs.
/// </summary>
internal sealed class MemberBindingCollection : IEnumerable<MemberBinding>
{
    private readonly Dictionary<string, MemberBinding> _bindings;

    /// <summary>
    /// Initializes a new instance of <see cref="MemberBindingCollection"/>.
    /// </summary>
    /// <param name="bindings">
    /// The type system member bindings.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="bindings"/> is <c>null</c>.
    /// </exception>
    public MemberBindingCollection(IEnumerable<MemberBinding> bindings)
    {
        if (bindings is null)
        {
            throw new ArgumentNullException(nameof(bindings));
        }

        _bindings = bindings.ToDictionary(t => t.SubgraphName, StringComparer.Ordinal);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MemberBindingCollection"/>.
    /// </summary>
    /// <param name="bindings">
    /// The type system member bindings.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="bindings"/> is <c>null</c>.
    /// </exception>
    public MemberBindingCollection(Dictionary<string, MemberBinding> bindings)
    {
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
    }

    /// <summary>
    /// Gets the number of bindings in this collection.
    /// </summary>
    public int Count => _bindings.Count;

    /// <summary>
    /// Gets the type system member binding for the specified subgraph.
    /// </summary>
    /// <param name="subgraph">
    /// The name of the subgraph.
    /// </param>
    public MemberBinding this[string subgraph]
        => _bindings[subgraph];

    /// <summary>
    /// Tries to get the type system member binding for the specified subgraph.
    /// </summary>
    /// <param name="subgraph">
    /// The name of the subgraph.
    /// </param>
    /// <param name="binding">
    /// The type system member binding.
    /// </param>
    /// <returns>
    /// <c>true</c> if the binding could be found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetBinding(string subgraph, [NotNullWhen(true)] out MemberBinding? binding)
        => _bindings.TryGetValue(subgraph, out binding);

    /// <summary>
    /// Determines whether this collection contains a binding for the specified subgraph.
    /// </summary>
    /// <param name="subgraph">
    /// The name of the subgraph.
    /// </param>
    /// <returns>
    /// <c>true</c> if the binding could be found; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsSubgraph(string subgraph)
        => _bindings.ContainsKey(subgraph);

    /// <summary>
    /// Gets an enumerator that iterates through this collection.
    /// </summary>
    public IEnumerator<MemberBinding> GetEnumerator() => _bindings.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets an empty collection of type system member bindings.
    /// </summary>
    public static MemberBindingCollection Empty { get; } =
        new(new List<MemberBinding>());
}
