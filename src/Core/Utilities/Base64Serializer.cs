using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace HotChocolate.Utilities
{
    public static class Base64Serializer
    {
        public static string Serialize<T>(T obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(buffer);
        }

        public static T Deserialize<T>(string s)
        {
            byte[] buffer = Convert.FromBase64String(s);
            string json = Encoding.UTF8.GetString(buffer);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
