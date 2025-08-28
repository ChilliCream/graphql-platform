namespace ChilliCream.Nitro.CommandLine.Cloud.Results;

internal interface IResultFormatter
{
    bool CanHandle(OutputFormat format);

    void Format(Result result);
}
