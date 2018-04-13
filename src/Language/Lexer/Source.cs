using System;
using System.IO;

namespace HotChocolate.Language
{
    public sealed class Source
        : ISource
    {
        public Source(string body)
        {
            // the normalization might be problematic. 
            // the line count should still work
            Text = body ?? string.Empty;
            Text = Text.Replace("\r\n", "\n")
                .Replace("\n\r", "\n");
        }

        public string Text { get; }
    }
}