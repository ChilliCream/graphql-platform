using System.Diagnostics;
using System.Text;

namespace HotChocolate.Fusion.Execution;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class ErrorTrie : Dictionary<object, ErrorTrie>
{
    public List<IError>? Errors { get; private set; }

    public static ErrorTrie FromErrors(List<IError> errors)
    {
        var root = new ErrorTrie();

        foreach (var error in errors)
        {
            if (error.Path is null)
            {
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
                    currentTrie.PushError(error);
                }
            }
        }

        return root;
    }

    private void PushError(IError error)
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

    private string GetDebuggerDisplay()
    {
        StringBuilder sb = new();

        var hasErrors = Errors?.Count > 0;

        if (hasErrors)
        {
            sb.Append($"Errors: {Errors!.Count}");
        }

        if (Keys.Count > 0)
        {
            if (hasErrors)
            {
                sb.Append(" - ");
            }

            var subPathSegments = string.Join(", ", Keys);

            sb.Append($"Sub paths: {subPathSegments}");
        }

        return sb.ToString();
    }
}
