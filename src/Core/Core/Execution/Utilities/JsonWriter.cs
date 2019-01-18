using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HotChocolate.Utilities
{
    static public partial class JsonWriter
    {
        static public void WriteValue(string value, Stream stream)
        {
            stream.Append(JsonConstants.DoubleQuote);
            stream.Append(Encoding.UTF8.GetBytes(value));
            stream.Append(JsonConstants.DoubleQuote);
        }

        static public void WriteValue(bool value, Stream stream)
        {
            if (value)
                stream.Append(JsonConstants.True);
            else
                stream.Append(JsonConstants.False);
        }

        static public void WriteValue(int value, Stream stream)
        {
            if (value < 0)
                stream.Append(JsonConstants.Dash);

            var array = NumberUtilities.numberToArray(Math.Abs(value));
            foreach (long elem in array)
            {
                stream.Append((byte)(elem + 48));
            }
        }

        static public void WriteValue(long value, Stream stream)
        {
            if (value < 0)
                stream.Append(JsonConstants.Dash);

            var array = NumberUtilities.numberToArray(Math.Abs(value));
            foreach (long elem in array)
            {
                stream.Append((byte)(elem + 48));
            }
        }

        static public void WriteValue(float value, Stream stream)
        {
            var str = value.ToString(CultureInfo.InvariantCulture);
            var strByteArray = Encoding.UTF8.GetBytes(str);
            stream.Append(strByteArray);
        }

        static public void WriteValue(double value, Stream stream)
        {
            var str = value.ToString(CultureInfo.InvariantCulture);
            var strByteArray = Encoding.UTF8.GetBytes(str);
            stream.Append(strByteArray);
        }

        static public void WriteValue(decimal value, Stream stream)
        {
            var str = value.ToString(CultureInfo.InvariantCulture);
            var strByteArray = Encoding.UTF8.GetBytes(str);
            stream.Append(strByteArray);
        }
    }
}
