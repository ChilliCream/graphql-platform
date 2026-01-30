using System.Text.Json;
using Spectre.Console.Json;

namespace ChilliCream.Nitro.CommandLine.Results;

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
        var obj = objectResult.Value;

        var serializedObj = JsonSerializer.Serialize(obj, obj.GetType(), JsonSourceGenerationContext.Default);

        console.Write(serializedObj);
    }
}
