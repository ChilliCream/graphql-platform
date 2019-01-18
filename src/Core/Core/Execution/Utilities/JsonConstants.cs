using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using HotChocolate.Validation;

namespace HotChocolate.Utilities
{
    internal static class JsonConstants
    {
        public const byte DoubleQuote = (byte)'"';
        public const byte LeftBrace = (byte)'{';
        public const byte RightBrace = (byte)'}';
        public const byte LeftBracket = (byte)'[';
        public const byte RightBracket = (byte)']';
        public const byte Colon = (byte)':';
        public const byte Comma = (byte)',';
        public const byte Dash = (byte)'-';
        public const byte Period = (byte)'.';

        public readonly static byte[] Null = Encoding.UTF8.GetBytes("null");
        public readonly static byte[] True = Encoding.UTF8.GetBytes("true");
        public readonly static byte[] False = Encoding.UTF8.GetBytes("false");
    }
}
