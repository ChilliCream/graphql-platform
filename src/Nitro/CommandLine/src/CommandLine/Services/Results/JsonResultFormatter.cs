using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Results;

internal sealed class JsonResultFormatter(INitroConsole console) : IResultFormatter
{
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

        console.Out.WriteLine(serializedObj);
    }
}
