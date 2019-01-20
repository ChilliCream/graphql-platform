using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Utilities
{
    internal static class ObjectToJsonBytes
    {
        internal static void WriteObjectToStream(object value, Stream stream)
        {
            Write(value, stream);
        }

        internal static void WriteKeyToStream(string value, Stream stream)
        {
            WriteKey(value, stream);
        }

        static void WriteKey(string value, Stream stream)
        {
            Write(value, stream);
            stream.Append(JsonConstants.Colon);
        }

        static void WriteObject(IDictionary<string, object> dictionary, Stream stream)
        {
            var useDelimiter = false;
            stream.Append(JsonConstants.LeftBrace);
            foreach (KeyValuePair<string, object> field in dictionary)
            {
                if (useDelimiter)
                {
                    stream.Append(JsonConstants.Comma);
                }
                WriteField(field, stream);
                useDelimiter = true;
            }
            stream.Append(JsonConstants.RightBrace);
        }

        static void WriteField(KeyValuePair<string, object> field, Stream stream)
        {
            WriteKey(field.Key, stream);
            Write(field.Value, stream);
        }

        static void WriteList(IList<object> list, Stream stream)
        {
            var useDelimiter = false;
            stream.Append(JsonConstants.LeftBracket);
            for (int i = 0; i < list.Count; i++)
            {
                if (useDelimiter)
                {
                    stream.Append(JsonConstants.Comma);
                }
                Write(list[i], stream);
                useDelimiter = true;
            }
            stream.Append(JsonConstants.RightBracket);
        }

        static void WriteErrorList(IList<IError> list, Stream stream)
        {
            var useDelimiter = false;
            stream.Append(JsonConstants.LeftBracket);
            for (int i = 0; i < list.Count; i++)
            {
                if (useDelimiter)
                {
                    stream.Append(JsonConstants.Comma);
                }
                Write(list[i], stream);
                useDelimiter = true;
            }
            stream.Append(JsonConstants.RightBracket);
        }

        static void WriteValue(object obj, Stream stream)
        {
            if (obj == null)
            {
                stream.Append(JsonConstants.Null);
            }
            else
            {
                Type type = obj.GetType();
                MethodInfo method = typeof(JsonWriter)
                    .GetMethod("WriteValue", new Type[] { type, typeof(Stream) });
                method.Invoke(null, new[] { obj, stream });
            }
        }

        static void Write(object value, Stream stream)
        {
            if (value is IDictionary<string, object> dictionary)
            {
                WriteObject(dictionary, stream);
            }
            else if (value is IList<object> list)
            {
                WriteList(list, stream);
            }
            else if (value is IList<IError> errorList)
            {
                WriteErrorList(errorList, stream);
            }
            else if (value is Array array)
            {
                WriteArray(array, stream);
            }
            else
            {
                WriteValue(value, stream);
            }
        }

        static void WriteArray(Array array, Stream stream)
        {
            var useDelimiter = false;
            stream.Append(JsonConstants.LeftBracket);
            for (int i = 0; i < (array as Array).Length; i++)
            {
                if (useDelimiter)
                {
                    stream.Append(JsonConstants.Comma);
                }
                Write(array.GetValue(i), stream);
                useDelimiter = true;
            }
            stream.Append(JsonConstants.RightBracket);
        }
    }
}
