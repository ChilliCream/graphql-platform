using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HotChocolate.Validation;
using Newtonsoft.Json;

namespace HotChocolate.Utilities
{
    static public partial class JsonWriter
    {
        static public void WriteValue(IError value, Stream stream)
        {
            var count = 1;
            PropertyInfo[] props = value.GetType().GetProperties();
            stream.Append(JsonConstants.LeftBrace);
            foreach (PropertyInfo prop in props)
            {
                if (prop.GetCustomAttribute(typeof(JsonPropertyAttribute)) != null)
                {
                    if (count > 1)
                        stream.Append(JsonConstants.Comma);
                    ObjectToJsonBytes.WriteKeyToStream(prop.Name.ToLowerInvariant(), stream);
                    ObjectToJsonBytes.WriteObjectToStream(prop.GetValue(value), stream);
                    count++;
                }
            }
            stream.Append(JsonConstants.RightBrace);
        }

        static public void WriteValue(Location value, Stream stream)
        {
            var count = 1;
            PropertyInfo[] props = value.GetType().GetProperties();
            stream.Append(JsonConstants.LeftBrace);
            foreach (PropertyInfo prop in props)
            {
                if (prop.GetCustomAttribute(typeof(JsonPropertyAttribute)) != null)
                {
                    if (count > 1)
                        stream.Append(JsonConstants.Comma);
                    ObjectToJsonBytes.WriteKeyToStream(prop.Name.ToLowerInvariant(), stream);
                    ObjectToJsonBytes.WriteObjectToStream(prop.GetValue(value), stream);
                    count++;
                }
            }
            stream.Append(JsonConstants.RightBrace);
        }

        static public void WriteValue(IReadOnlyCollection<string> stack, Stream stream)
        {
            var count = 1;

            stream.Append(JsonConstants.LeftBracket);
            foreach (var elem in stack)
            {
                if (count > 1)
                    stream.Append(JsonConstants.Comma);
                ObjectToJsonBytes.WriteObjectToStream(elem, stream);
                count++;
            }
            stream.Append(JsonConstants.RightBracket);
        }
    }
}
