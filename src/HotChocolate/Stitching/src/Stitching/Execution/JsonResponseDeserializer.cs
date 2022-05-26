using System;
using System.Collections.Generic;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Transport.Sockets.Client;

namespace HotChocolate.Stitching.Execution;

// This helper class is just introduced as a temporary crutch while we renovate other parts of
// the stitching layer.
internal static class JsonResponseDeserializer
{
    public static IQueryResult Deserialize(
        OperationResult operationResult)
    {
        var result = new QueryResultBuilder();

        if (operationResult.Data?.ValueKind is JsonValueKind.Object)
        {
            result.SetData(DeserializeObject(operationResult.Data.Value));
        }

        if (operationResult.Errors?.ValueKind is JsonValueKind.Array)
        {
            DeserializeErrors(result, operationResult.Errors.Value);
        }

        return result.Create();
    }

    private static void DeserializeErrors(
        IQueryResultBuilder result,
        JsonElement errors)
    {
        foreach (JsonElement error in errors.EnumerateArray())
        {
            var builder = ErrorBuilder.New();
            DeserializeErrorObject(builder, error);
            result.AddError(builder.Build());
        }
    }

    private static void DeserializeErrorObject(
        IErrorBuilder errorBuilder,
        JsonElement error)
    {
        errorBuilder.SetMessage(error.GetProperty("message").GetString()!);

        if (error.TryGetProperty("path", out JsonElement path))
        {
            DeserializeErrorPath(errorBuilder, path);
        }

        if (error.TryGetProperty("extensions", out JsonElement extensions))
        {
            foreach (JsonProperty property in extensions.EnumerateObject())
            {
                errorBuilder.SetExtension(property.Name, DeserializeErrorValue(property.Value));
            }
        }
    }

    private static void DeserializeErrorPath(
        IErrorBuilder errorBuilder,
        JsonElement path)
    {
        if (path.ValueKind is JsonValueKind.Array)
        {
            Path current = Path.Root;

            foreach (JsonElement segment in path.EnumerateArray())
            {
                switch (segment.ValueKind)
                {
                    case JsonValueKind.String:
                        current = PathFactory.Instance.Append(current, segment.GetString()!);
                        break;

                    case JsonValueKind.Number:
                        current = PathFactory.Instance.Append(current, segment.GetInt32());
                        break;

                    default:
                        throw new NotSupportedException("Path segments must be ints or strings.");
                }
            }

            errorBuilder.SetPath(current);
        }
    }

    private static object? DeserializeErrorValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();

                foreach (JsonProperty property in value.EnumerateObject())
                {
                    obj[property.Name] = DeserializeErrorValue(property.Value);
                }

                return obj;

            case JsonValueKind.Array:
                var list = new List<object?>();

                foreach (JsonElement element in value.EnumerateArray())
                {
                    list.Add(DeserializeValue(element));
                }

                return list;

            case JsonValueKind.String:
                return value.GetString();

            case JsonValueKind.Number:
                if (value.TryGetInt32(out var i))
                {
                    return i;
                }

                if (value.TryGetDouble(out var d))
                {
                    return d;
                }

                return value.GetRawText();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                throw new NotSupportedException();
        }
    }

    private static Dictionary<string, object?> DeserializeObject(JsonElement value)
    {
        var obj = new Dictionary<string, object?>();

        foreach (JsonProperty property in value.EnumerateObject())
        {
            obj[property.Name] = DeserializeValue(property.Value);
        }

        return obj;
    }

    private static object? DeserializeValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                return DeserializeObject(value);

            case JsonValueKind.Array:
                var list = new List<object?>();

                foreach (JsonElement element in value.EnumerateArray())
                {
                    list.Add(DeserializeValue(element));
                }

                return list;

            case JsonValueKind.String:
                return new StringValueNode(value.GetString()!);

            case JsonValueKind.Number:
                return Utf8GraphQLParser.Syntax.ParseValueLiteral(value.GetRawText());

            case JsonValueKind.True:
                return BooleanValueNode.True;

            case JsonValueKind.False:
                return BooleanValueNode.False;

            case JsonValueKind.Null:
                return NullValueNode.Default;

            default:
                throw new NotSupportedException();
        }
    }
}
