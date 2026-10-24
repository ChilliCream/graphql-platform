namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// A trie (prefix tree) data structure for efficiently organizing and retrieving GraphQL errors
/// based on their field paths. The <see cref="ErrorTrie"/> enables fast error lookups and
/// maintains the hierarchical structure of GraphQL error paths in a federated GraphQL request.
/// </summary>
/// <remarks>
/// <para>
/// Each node in the trie represents a path segment (field name or list index) and can
/// optionally contain an error. In GraphQL each path can have a single error.
/// The tree structure mirrors the GraphQL response structure,
/// allowing errors to be precisely located and retrieved.
/// </para>
/// <example>
///
/// For a GraphQL query structure like:
/// <code>
/// query {
///   user {
///     posts {
///       title
///       comments {
///         text
///       }
///     }
///   }
/// }
/// </code>
///
/// An error at path ["user", "posts", 0, "title"] would create the trie structure:
/// <code>
/// root["user"]["posts"][0]["title"] = error
/// </code>
/// </example>
/// </remarks>
public sealed class ErrorTrie : Dictionary<object, ErrorTrie>
{
    /// <summary>
    /// Gets or sets the error associated with this node in the trie.
    /// </summary>
    /// <value>
    /// The <see cref="IError"/> instance if an error exists at this path,
    /// otherwise <c>null</c>.
    /// </value>
    /// <remarks>
    /// An error is stored at the final node of a path. Intermediate nodes
    /// typically have a <c>null</c> error value and serve as path segments
    /// leading to the actual error location.
    /// </remarks>
    public IError? Error { get; set; }

    internal bool TryGetIndex(int index, out ErrorTrie? errorTrie)
    {
        if (Count == 0)
        {
            errorTrie = null;
            return false;
        }

        if (Count == 1 && EnsureCapacity(0) <= 3)
        {
            using var entries = GetEnumerator();
            entries.MoveNext();
            var entry = entries.Current;

            if (entry.Key is int existingIndex)
            {
                if (existingIndex == index)
                {
                    errorTrie = entry.Value;
                    return true;
                }

                errorTrie = null;
                return false;
            }
        }

        return TryGetValue(index, out errorTrie);
    }

    /// <summary>
    /// Finds and returns the first error encountered in the trie using depth-first traversal.
    /// </summary>
    public IError? FindFirstError()
    {
        if (Error is not null)
        {
            return Error;
        }

        var stack = new Stack<ErrorTrie>(Values);

        while (stack.TryPop(out var errorTrie))
        {
            if (errorTrie.Error is not null)
            {
                return errorTrie.Error;
            }

            foreach (var child in errorTrie.Values)
            {
                stack.Push(child);
            }
        }

        return null;
    }
}
