namespace ChilliCream.Nitro.CommandLine.Results;

internal sealed class ResultHolder : IResultHolder
{
    public Result? Result { get; private set; }

    public void SetResult(Result result)
    {
        Result = result;
    }
}
