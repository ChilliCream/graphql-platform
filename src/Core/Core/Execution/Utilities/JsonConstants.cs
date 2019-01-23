using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using HotChocolate.Validation;

namespace HotChocolate.Execution
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
        public const byte Backspace = (byte) '\b';
        public const byte CarriageReturn = (byte)'\r';
        public const byte LineFeed = (byte)'\n';
        public const byte Tab = (byte)'\t';
        public const byte Backslash = (byte)'\\';
        public const byte ReverseBackslash = (byte)'/';
        public const byte FormFeed = (byte)'\f';

        public readonly static byte[] Null = new byte[] { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
        public readonly static byte[] True = new byte[] { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
        public readonly static byte[] False = new byte[] { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
    }
}
