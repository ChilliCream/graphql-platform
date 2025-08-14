namespace ChilliCream.Nitro.CLI.Results;

internal sealed class JsonResultFormatter(IAnsiConsole console) : IResultFormatter
{
    public bool CanHandle(OutputFormat format) => format == OutputFormat.Json;

    public void Format(Result result)
    {
        switch (result)
        {
            case ObjectResult objectResult:
                FormatObjectResult(objectResult);
                break;
        }
    }

    private void FormatObjectResult(ObjectResult objectResult)
    {
        console.Json(objectResult.Value);
    }
}
