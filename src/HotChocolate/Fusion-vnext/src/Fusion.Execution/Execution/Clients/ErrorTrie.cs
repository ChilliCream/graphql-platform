namespace HotChocolate.Fusion.Execution.Clients;

public sealed class ErrorTrie : Dictionary<object, ErrorTrie>
{
    public IError? Error { get; set; }

    public (object[] Path, IError Error)? FindPathToFirstError()
    {
        if (Error is not null)
        {
            return ([], Error);
        }

        var stack = new Stack<(ErrorTrie Node, List<object> Path)>();

        foreach (var kvp in this)
        {
            stack.Push((kvp.Value, [kvp.Key]));
        }

        while (stack.Count > 0)
        {
            var (node, path) = stack.Pop();

            if (node.Error is not null)
            {
                return ([..path], node.Error);
            }

            foreach (var kvp in node)
            {
                stack.Push((kvp.Value, [..path, kvp.Key]));
            }
        }

        return null;
    }
}
