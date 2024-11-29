using System.Text.Json;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake.Json;

public static class JsonErrorParser
{
    public static IReadOnlyList<IClientError>? ParseErrors(JsonElement result)
    {
        if (result is { ValueKind: JsonValueKind.Array, } errors)
        {
            var array = new IClientError[errors.GetArrayLength()];
            var i = 0;

            foreach (var error in errors.EnumerateArray())
            {
                try
                {
                    array[i] = ParseError(error);
                }
                catch (Exception ex)
                {
                    array[i] = new ClientError(
                        JsonErrorParser_ParseErrors_Error,
                        exception: ex);
                }
                i++;
            }

            return array;
        }

        return null;
    }

    private static IClientError ParseError(JsonElement error)
    {
        var message = error.GetPropertyOrNull("message")?.GetString();

        if (message is null)
        {
            return new ClientError(JsonErrorParser_ParseError_MessageCannotBeNull);
        }

        var path = error.ParsePath();
        var locations = error.ParseLocations();
        var extensions = error.ParseExtensions();
        var code = error.ParseCode(extensions);

        return new ClientError(message, code, path, locations, extensions: extensions);
    }

    private static IReadOnlyList<object>? ParsePath(this JsonElement error)
    {
        if (error.GetPropertyOrNull("path") is { ValueKind: JsonValueKind.Array, } path)
        {
            var array = new object[path.GetArrayLength()];
            var i = 0;

            foreach (var element in path.EnumerateArray())
            {
                array[i++] = element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString()!,
                    JsonValueKind.Number => element.GetInt32(),
                    _ => "NOT_SUPPORTED_VALUE",
                };
            }

            return array;
        }

        return null;
    }

    private static IReadOnlyList<Location>? ParseLocations(this JsonElement error)
    {
        if (error.GetPropertyOrNull("locations") is { ValueKind: JsonValueKind.Array, } locations)
        {
            var array = new Location[locations.GetArrayLength()];
            var i = 0;

            foreach (var location in locations.EnumerateArray())
            {
                array[i++] = new Location(
                    location.GetProperty("line").GetInt32(),
                    location.GetProperty("column").GetInt32());
            }

            return array;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, object?>? ParseExtensions(this JsonElement error)
    {
        if (error.GetPropertyOrNull("extensions") is { ValueKind: JsonValueKind.Object, } ext)
        {
            return (IReadOnlyDictionary<string, object?>?)ParseValue(ext);
        }

        return null;
    }

    private static string? ParseCode(
        this JsonElement error,
        IReadOnlyDictionary<string, object?>? extensions)
    {
        // if we have a top level code property we will take this.
        // While this is not spec compliant with the 2018 spec many people still do this.
        if (error.GetPropertyOrNull("code") is { ValueKind: JsonValueKind.String, } code)
        {
            return code.GetString();
        }

        // if we do not have a top level error code we will check if the extensions
        // dictionary has any field called code.
        if (extensions is not null && extensions.TryGetValue("code", out var value) &&
            value is string s)
        {
            return s;
        }

        return null;
    }

    private static object? ParseValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();

                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = ParseValue(property.Value);
                }

                return dict;

            case JsonValueKind.Array:
                var array = new object?[element.GetArrayLength()];
                var i = 0;

                foreach (var item in element.EnumerateArray())
                {
                    array[i++] = ParseValue(item);
                }

                return array;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                {
                    return intValue;
                }

                if (element.TryGetDouble(out var floatValue))
                {
                    return floatValue;
                }

                return null;

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            default:
                return null;
        }
    }
}
