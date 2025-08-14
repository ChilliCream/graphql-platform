namespace ChilliCream.Nitro.CLI.Results;

internal interface IResultFormatter
{
    bool CanHandle(OutputFormat format);

    void Format(Result result);
}
