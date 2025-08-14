namespace HotChocolate.Fusion.Execution.Clients;

public sealed class ErrorTrie : Dictionary<object, ErrorTrie>
{
    public IError? Error { get; set; }

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
