using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Results;

internal sealed class JsonResultFormatter(INitroConsole console) : IResultFormatter
{
    public void Format(Result result)
    {
        switch (result)
        {
            case ObjectResult objectResult:
                Serialize(objectResult.Value);
                break;

            default:
                Serialize(result);
                break;
        }
    }

    private void Serialize(object value)
    {
        var serializedObj = JsonSerializer.Serialize(value, value.GetType(), JsonSourceGenerationContext.Default);

        console.Out.WriteLine(serializedObj);
    }
}
