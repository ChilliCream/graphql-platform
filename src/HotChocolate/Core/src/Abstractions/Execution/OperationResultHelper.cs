namespace HotChocolate.Execution;

internal static class OperationResultHelper
{
    private const string Data = "data";
    private const string Errors = "errors";
    private const string Extensions = "extensions";
    private const string Message = "message";
    private const string Locations = "locations";
    private const string Path = "path";
    private const string Line = "line";
    private const string Column = "column";

    public static IReadOnlyDictionary<string, object?> ToDictionary(IOperationResult result)
    {
        var formatted = new OrderedDictionary<string, object?>();

        if (result.Errors is { Count: > 0, })
        {
            formatted[Errors] = SerializeErrors(result.Errors);
        }

        if (result.Data is { Count: > 0, })
        {
            formatted[Data] = result.Data;
        }

        if (result.Extensions is { Count: > 0, })
        {
            formatted[Extensions] = result.Extensions;
        }

        return formatted;
    }

    private static ICollection<object> SerializeErrors(
        IReadOnlyCollection<IError> errors)
    {
        var formattedErrors = new List<object>();

        foreach (var error in errors)
        {
            var formattedError = new OrderedDictionary<string, object?> { [Message] = error.Message, };

            if (error.Locations is { Count: > 0, })
            {
                formattedError[Locations] = SerializeLocations(error.Locations);
            }

            if (error.Path is { })
            {
                formattedError[Path] = error.Path.ToList();
            }

            if (error.Extensions is { Count: > 0, })
            {
                formattedError[Extensions] = error.Extensions;
            }

            formattedErrors.Add(formattedError);
        }

        return formattedErrors;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, int>> SerializeLocations(
        IReadOnlyList<Location> locations)
    {
        var serializedLocations = new IReadOnlyDictionary<string, int>[locations.Count];

        for (var i = 0; i < locations.Count; i++)
        {
            var location = locations[i];
            serializedLocations[i] = new OrderedDictionary<string, int>
                {
                    { Line, location.Line },
                    { Column, location.Column },
                };
        }

        return serializedLocations;
    }
}
