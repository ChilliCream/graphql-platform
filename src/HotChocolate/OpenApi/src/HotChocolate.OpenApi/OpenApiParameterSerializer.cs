using System.Text;
using HotChocolate.OpenApi.Helpers;
using Microsoft.OpenApi.Models;

namespace HotChocolate.OpenApi;

internal static class OpenApiParameterSerializer
{
    private const string DelimiterAmpersand = "&";
    private const string DelimiterComma = ",";
    private const string DelimiterPipe = "|";
    private const string DelimiterSpace = "%20"; // Escaped

    public static string SerializeParameter(OpenApiParameter parameter, object? value)
    {
        var escape = parameter is not { In: ParameterLocation.Query, AllowReserved: true };

        return value switch
        {
            Dictionary<string, object?> dictionary =>
                SerializeParameter(
                    parameter,
                    dictionary,
                    escape),

            IEnumerable<object?> list =>
                SerializeParameter(
                    parameter,
                    list,
                    escape),

            _ =>
                SerializeParameter(
                    parameter.Style ?? ParameterStyle.Simple,
                    parameter.Name,
                    value?.ToString() ?? "",
                    escape),
        };
    }

    private static string SerializeParameter(
        OpenApiParameter parameter,
        Dictionary<string, object?> value,
        bool escape)
    {
        return parameter.Style switch
        {
            ParameterStyle.Matrix =>
                SerializeObjectParameter(
                    ParameterStyle.Matrix,
                    parameter.Explode,
                    parameter.Name,
                    value: parameter.Explode
                        ? value
                        : new Dictionary<string, object?>
                        {
                            {
                                parameter.Name,
                                SerializeObjectParameter(
                                    ParameterStyle.Simple,
                                    parameter.Explode,
                                    parameter.Name,
                                    value,
                                    DelimiterComma,
                                    escape)
                            },
                        },
                    delimiter: null,
                    escape: escape && parameter.Explode),

            ParameterStyle.Label =>
                SerializeObjectParameter(
                    ParameterStyle.Label,
                    parameter.Explode,
                    parameter.Name,
                    value,
                    delimiter: null,
                    escape: escape),

            ParameterStyle.Form =>
                SerializeObjectParameter(
                    ParameterStyle.Form,
                    parameter.Explode,
                    parameter.Name,
                    value: parameter.Explode
                        ? value
                        : new Dictionary<string, object?>
                        {
                            {
                                parameter.Name,
                                SerializeObjectParameter(
                                    ParameterStyle.Simple,
                                    parameter.Explode,
                                    parameter.Name,
                                    value,
                                    DelimiterComma,
                                    escape)
                            },
                        },
                    DelimiterAmpersand,
                    escape: escape && parameter.Explode),

            ParameterStyle.Simple =>
                SerializeObjectParameter(
                    ParameterStyle.Simple,
                    parameter.Explode,
                    parameter.Name,
                    value,
                    DelimiterComma,
                    escape: escape),

            ParameterStyle.SpaceDelimited =>
                SerializeObjectParameter(
                    ParameterStyle.SpaceDelimited,
                    parameter.Explode,
                    parameter.Name,
                    value,
                    DelimiterSpace,
                    escape: escape),

            ParameterStyle.PipeDelimited =>
                SerializeObjectParameter(
                    ParameterStyle.PipeDelimited,
                    parameter.Explode,
                    parameter.Name,
                    value,
                    DelimiterPipe,
                    escape: escape),

            ParameterStyle.DeepObject =>
                SerializeObjectParameter(
                    ParameterStyle.DeepObject,
                    parameter.Explode,
                    parameter.Name,
                    value,
                    DelimiterAmpersand,
                    escape: escape),

            _ => throw new InvalidOperationException(),
        };
    }

    private static string SerializeParameter(
        OpenApiParameter parameter,
        IEnumerable<object?> values,
        bool escape)
    {
        return parameter.Style switch
        {
            ParameterStyle.Matrix =>
                SerializeListParameter(
                    style: ParameterStyle.Matrix,
                    name: parameter.Name,
                    values: parameter.Explode
                        ? values
                        : [SerializeListParameter(
                            ParameterStyle.Simple,
                            parameter.Name,
                            values,
                            DelimiterComma,
                            escape)],
                    delimiter: null,
                    escape: escape && parameter.Explode),

            ParameterStyle.Label =>
                SerializeListParameter(
                    ParameterStyle.Label,
                    parameter.Name,
                    values,
                    delimiter: null,
                    escape),

            ParameterStyle.Form =>
                SerializeListParameter(
                    style: ParameterStyle.Form,
                    parameter.Name,
                    values: parameter.Explode ? values : [
                        SerializeListParameter(
                            ParameterStyle.Simple,
                            parameter.Name,
                            values,
                            DelimiterComma,
                            escape)],
                    DelimiterAmpersand,
                    escape: escape && parameter.Explode),

            ParameterStyle.Simple =>
                SerializeListParameter(
                    ParameterStyle.Simple,
                    parameter.Name,
                    values,
                    DelimiterComma,
                    escape),

            ParameterStyle.SpaceDelimited =>
                SerializeListParameter(
                    ParameterStyle.Simple,
                    parameter.Name,
                    values,
                    DelimiterSpace,
                    escape),

            ParameterStyle.PipeDelimited =>
                SerializeListParameter(
                    ParameterStyle.Simple,
                    parameter.Name,
                    values,
                    DelimiterPipe,
                    escape),

            ParameterStyle.DeepObject => throw new InvalidOperationException(),

            _ => throw new InvalidOperationException(),
        };
    }

    private static string SerializeParameter(
        ParameterStyle style,
        string name,
        string value,
        bool escape)
    {
        value = escape ? UriHelper.EscapeDataStringRfc3986(value) : value;

        return style switch
        {
            ParameterStyle.Matrix => value is "" ? $";{name}" : $";{name}={value}",
            ParameterStyle.Label => $".{value}",
            ParameterStyle.Form => $"{name}={value}",
            _ => value,
        };
    }

    private static string SerializeObjectParameter(
        ParameterStyle style,
        bool explode,
        string name,
        Dictionary<string, object?> value,
        string? delimiter,
        bool escape)
    {
        if (value.Count is 0)
        {
            return "";
        }

        var firstKey = value.First().Key;
        var firstString = SerializeKeyValuePair(
            style,
            explode,
            style is ParameterStyle.DeepObject ? $"{name}[{firstKey}]" : firstKey,
            value.First().Value?.ToString() ?? "",
            escape);

        if (value.Count is 1)
        {
            return firstString;
        }

        var stringBuilder = new StringBuilder(firstString);

        for (var i = 1; i < value.Count; ++i)
        {
            if (delimiter is not null)
            {
                stringBuilder.Append(delimiter);
            }

            var element = value.ElementAt(i);

            stringBuilder.Append(
                SerializeKeyValuePair(
                    style,
                    explode,
                    style is ParameterStyle.DeepObject ? $"{name}[{element.Key}]" : element.Key,
                    element.Value?.ToString() ?? "",
                    escape));
        }

        return stringBuilder.ToString();
    }

    private static string SerializeKeyValuePair(
        ParameterStyle style,
        bool explode,
        string key,
        string value,
        bool escape)
    {
        value = escape ? UriHelper.EscapeDataStringRfc3986(value) : value;

        return style switch
        {
            ParameterStyle.Matrix => value is "" ? $";{key}" : $";{key}={value}",
            ParameterStyle.Label => explode ? $".{key}={value}" : $".{key}.{value}",
            ParameterStyle.Form => $"{key}={value}",
            ParameterStyle.SpaceDelimited => $"{key}%20{value}",
            ParameterStyle.PipeDelimited => $"{key}|{value}",
            _ => explode ? $"{key}={value}" : $"{key},{value}",
        };
    }

    private static string SerializeListParameter(
        ParameterStyle style,
        string name,
        IEnumerable<object?> values,
        string? delimiter,
        bool escape = true)
    {
        var list = values.ToList();

        if (list.Count is 0)
        {
            return "";
        }

        var firstString = SerializeParameter(style, name, list[0]?.ToString() ?? "", escape);

        if (list.Count is 1)
        {
            return firstString;
        }

        var stringBuilder = new StringBuilder(firstString);

        for (var i = 1; i < list.Count; ++i)
        {
            if (delimiter is not null)
            {
                stringBuilder.Append(delimiter);
            }

            var value = list[i];

            stringBuilder.Append(SerializeParameter(style, name, value?.ToString() ?? "", escape));
        }

        return stringBuilder.ToString();
    }
}
