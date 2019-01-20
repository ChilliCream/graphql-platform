using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HotChocolate.Utilities
{
    internal static class JsonWriterHelper
    {
        internal static string ToJsonString(this string s)
        {
            var stringBuilder = new StringBuilder(s);

            return stringBuilder
                .Replace("\"", "\\\"")
                .Replace("\\", "\\\\")
                .Replace("/", "\\/")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .ToString();
        }
    }
}
