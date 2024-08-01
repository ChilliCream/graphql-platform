namespace StrawberryShake.Tools;

public class JsonConsoleOutputErrorData
{
    public JsonConsoleOutputErrorData(HotChocolate.IError error)
    {
        Message = error.Message;
        Code = error.Code!;

        if (error.Extensions is { } && error.Extensions.ContainsKey("fileName"))
        {
            FileName = $"{Path.GetFullPath((string)error.Extensions["fileName"]!)}";
        }

        if (error.Locations is { } && error.Locations.Count > 0)
        {
            Location = error.Locations[0];
        }
    }

    public string Message { get; }

    public string Code { get; }

    public string? FileName { get; }

    public HotChocolate.Location? Location { get; }
}
