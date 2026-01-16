namespace ChilliCream.Nitro.CommandLine.Results;

internal interface IResultFormatter
{
    bool CanHandle(OutputFormat format);

    void Format(Result result);
}
