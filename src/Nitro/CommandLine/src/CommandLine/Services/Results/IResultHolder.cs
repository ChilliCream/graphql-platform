namespace ChilliCream.Nitro.CommandLine.Results;

internal interface IResultHolder
{
    Result? Result { get; }
    
    void SetResult(Result result);
}
