namespace HotChocolate.Fusion.Execution;

// The key is either a string or an int depending on the path segment
public class ErrorTrie : Dictionary<object, ErrorTrie>
{
    public ErrorTrie()
    {
    }

    public ErrorTrie(List<IError> errors)
    {
        Errors = errors;
    }

    public List<IError>? Errors { get; private set; }

    public void PushError(IError error)
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

    public static ErrorTrie BuildFromErrors(List<IError> errors)
    {
        var root = new ErrorTrie();

        foreach (var error in errors)
        {
            if (error.Path is null)
            {
                root.PushError(error);
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
}
