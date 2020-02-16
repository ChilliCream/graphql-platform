using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace HotChocolate.Subscriptions
{
    [Obsolete("This type will be removed and has no replacement.")]
    public class JsonPayloadSerializer
        : IPayloadSerializer
    {
        private static readonly JsonSerializerSettings _settings =
            new JsonSerializerSettings
            {
#if NETSTANDARD2_0
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
#else
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,
#endif

                TypeNameHandling = TypeNameHandling.All,
                Culture = CultureInfo.InvariantCulture,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                ReferenceLoopHandling = ReferenceLoopHandling.Error
            };

        public byte[] Serialize(object value)
        {
            var json = JsonConvert.SerializeObject(value, _settings);
            byte[] encoded = Encoding.UTF8.GetBytes(json);
            return encoded;
        }

        public object Deserialize(byte[] content)
        {
            var json = Encoding.UTF8.GetString(content);
            object value = JsonConvert.DeserializeObject(json, _settings);
            return value;
        }
    }
}
