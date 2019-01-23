using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HotChocolate.Execution
{
    static public partial class JsonWriter
    {
        static public void WriteValue(string value, Stream stream)
        {
            stream.Append(JsonConstants.DoubleQuote);
            value = value.ToJsonString();
            stream.Append(Encoding.UTF8.GetBytes(value));
            stream.Append(JsonConstants.DoubleQuote);
        }

        static public void WriteValue(bool value, Stream stream)
        {
            if (value)
            {
                stream.Append(JsonConstants.True);
            }
            else
            {
                stream.Append(JsonConstants.False);
            }
        }

        static public void WriteValue(int value, Stream stream)
        {
            if (value < 0)
            {
                stream.Append(JsonConstants.Dash);
            }
            WriteIntegralType((ulong) Math.Abs(value), stream);
        }

        static public void WriteValue(uint value, Stream stream)
        {
            if (value < 0)
            {
                stream.Append(JsonConstants.Dash);
            }
            WriteIntegralType(value, stream);
        }

        static public void WriteValue(long value, Stream stream)
        {
            if (value < 0)
            {
                stream.Append(JsonConstants.Dash);
            }
            WriteIntegralType((ulong) Math.Abs(value), stream);
        }

        static public void WriteValue(ulong value, Stream stream)
        {
            if (value < 0)
            {
                stream.Append(JsonConstants.Dash);
            }
            WriteIntegralType(value, stream);
        }

        static public void WriteValue(float value, Stream stream)
        {
            var str = value.ToString("R", CultureInfo.InvariantCulture);
            var strByteArray = Encoding.UTF8.GetBytes(str);
            stream.Append(strByteArray);
        }

        static public void WriteValue(double value, Stream stream)
        {
            var str = value.ToString("G13", CultureInfo.InvariantCulture);
            var strByteArray = Encoding.UTF8.GetBytes(str);
            stream.Append(strByteArray);
        }

        static public void WriteValue(decimal value, Stream stream)
        {
            var str = value.ToString("G29", CultureInfo.InvariantCulture);
            var strByteArray = Encoding.UTF8.GetBytes(str);
            stream.Append(strByteArray);
        }

        static private void WriteIntegralType(ulong value, Stream stream)
        {
            var array = NumberUtilities.NumberToArray((ulong) value);
            for (var i = 0; i < array.Length; i++)
            {
                stream.Append((byte)(array[i] + 48));
            }
        }
    }
}
