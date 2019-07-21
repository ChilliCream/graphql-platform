using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HotChocolate.Subscriptions
{
    public class JsonPayloadSerializer
        : IPayloadSerializer
    {
        private static readonly JsonSerializerSettings _settings =
            new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling =
                    TypeNameAssemblyFormatHandling.Full,
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
