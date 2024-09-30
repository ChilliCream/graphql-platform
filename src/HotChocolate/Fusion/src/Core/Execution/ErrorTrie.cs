using System.Diagnostics;
using System.Text;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution;

internal class ErrorTrie : Dictionary<object, ErrorTrie>
{
    public List<IError>? Errors { get; private set; }

    /// <summary>
    /// Traverses this <see cref="ErrorTrie"/> and returns the first error it finds.
    /// </summary>
    public IError? GetFirstError()
    {
        var stack = new Stack<ErrorTrie>();
        stack.Push(this);

        while (stack.TryPop(out var currentErrorTrie))
        {
            if (currentErrorTrie.Errors?.FirstOrDefault() is { } error)
            {
                return error;
            }

            foreach (var value in currentErrorTrie.Values)
            {
                stack.Push(value);
            }
        }

        return null;
    }

    /// <summary>
    /// Traverses this <see cref="ErrorTrie"/> along the specified
    /// <paramref name="path"/>. If one of the items along the path has an error,
    /// the error is returned.
    /// If the path can't be followed or there are no errors on the path,
    /// the errors on this <see cref="ErrorTrie"/> are returned (if present).
    /// </summary>
    public IError? GetFirstErrorOnPath(string[] path)
    {
        var currentErrorTrie = this;
        foreach (var segment in path)
        {
            if (currentErrorTrie.TryGetValue(segment, out var childErrorTrie))
            {
                if (childErrorTrie.Errors?.FirstOrDefault() is { } error)
                {
                    return error;
                }

                currentErrorTrie = childErrorTrie;
            }
            else
            {
                break;
            }
        }

        return Errors?.FirstOrDefault();
    }

    /// <summary>
    /// Traverses this <see cref="ErrorTrie"/> along the specified
    /// <paramref name="path"/>. If one of the items along the path has an error,
    /// the error is returned.
    /// If the path can't be followed or there are no errors on the path,
    /// the errors on this <see cref="ErrorTrie"/> are returned (if present).
    /// </summary>
    public IError? GetFirstErrorBelowPath(string[] path)
    {
        var currentErrorTrie = this;
        foreach (var segment in path)
        {
            if (!currentErrorTrie.TryGetValue(segment, out currentErrorTrie))
            {
                return null;
            }
        }

        return currentErrorTrie.GetFirstError();
    }

    /// <summary>
    /// Creates an <see cref="ErrorTrie"/> from the given <paramref name="errors"/>,
    /// using the <see cref="Path"/> of each error to build the trie.
    /// </summary>
    /// <remarks>
    /// Errors without a path are added to the root of the trie.
    /// </remarks>
    public static ErrorTrie FromErrors(List<IError> errors)
    {
        var root = new ErrorTrie();

        foreach (var error in errors)
        {
            if (error.Path is null)
            {
                root.AddError(error);
                continue;
            }

            var pathSegments = error.Path.ToList();

            var currentTrie = root;
            var lastPathIndex = pathSegments.Count - 1;

            for (var i = 0; i < pathSegments.Count; i++)
            {
                var pathSegment = pathSegments[i];

                if (currentTrie.TryGetValue(pathSegment, out var trieAtPath))
                {
                    currentTrie = trieAtPath;
                }
                else
                {
                    var newTrie = new ErrorTrie();
                    currentTrie[pathSegment] = newTrie;
                    currentTrie = newTrie;
                }

                if (i == lastPathIndex)
                {
                    currentTrie.AddError(error);
                }
            }
        }

        return root;
    }

    /// <summary>
    /// Creates a new <see cref="ErrorTrie"/> with an entry for each item in
    /// <paramref name="rootSelections"/>, containing the specified <paramref name="error"/>.
    /// </summary>
    public static ErrorTrie FromSelections(IError error, List<RootSelection> rootSelections)
    {
        var errorTrie = new ErrorTrie();

        foreach (var rootSelection in rootSelections)
        {
            var errorTrieForSubfield = new ErrorTrie();
            errorTrieForSubfield.AddError(error);

            errorTrie.Add(rootSelection.Selection.ResponseName, errorTrieForSubfield);
        }

        return errorTrie;
    }

    // TODO: Better name and summary
    public static ErrorTrie? FromSelections(
        ErrorTrie subgraphErrorTrie,
        List<RootSelection> rootSelections,
        string[] path)
    {
        var firstErrorOnPath = subgraphErrorTrie.GetFirstErrorOnPath(path);

        return firstErrorOnPath is null ? null : FromSelections(firstErrorOnPath, rootSelections);
    }

    private void AddError(IError error)
    {
        if (Errors is null)
        {
            Errors = [error];
        }
        else
        {
            Errors.Add(error);
        }
    }
}
