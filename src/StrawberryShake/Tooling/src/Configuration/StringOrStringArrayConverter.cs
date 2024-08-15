using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StrawberryShake.Tools.Configuration;

public class StringOrStringArrayConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string[]) || objectType == typeof(string);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var token = JToken.Load(reader);

        return token.Type switch
        {
            JTokenType.String => [token.ToString()],
            JTokenType.Array => token.ToObject<string[]>() ??
                throw new JsonSerializationException("Unexpected array type"),

            _ => throw new JsonSerializationException("Unexpected JTokenType type")
        };
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not string[] stringArray)
        {
            throw new JsonSerializationException("The value is expected to be a string[]");
        }

        if (stringArray.Length == 1)
        {
            writer.WriteValue(stringArray[0]);
        }
        else
        {
            JArray.FromObject(stringArray).WriteTo(writer);
        }
    }
}
