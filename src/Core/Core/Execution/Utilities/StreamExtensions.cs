using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HotChocolate.Utilities
{
    internal static class StreamExtensions
    {
        static internal void Append(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        static internal void Append(this Stream stream, byte b)
        {
            stream.WriteByte(b);
        }
    }
}
