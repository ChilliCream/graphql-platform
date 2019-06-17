using System;
using Newtonsoft.Json;

namespace HotChocolate.Client.Internal
{
    /// <summary>
    /// Converts <see cref="ID"/>s to and from JSON strings.
    /// </summary>
    class IDConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(ID);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((ID)value).Value);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new ID(serializer.Deserialize<string>(reader));
        }
    }
}
