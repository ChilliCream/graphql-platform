using System;
using System.Collections.Generic;

namespace StrawberryShake.Tools
{
    public class DownloadCommandContext
    {
        public DownloadCommandContext(
            Uri uri,
            string fileName,
            string? token,
            string? scheme,
            Dictionary<string, IEnumerable<string>> customHeaders)
        {
            Uri = uri;
            FileName = fileName;
            Token = token;
            Scheme = scheme;
            CustomHeaders = customHeaders;
        }

        public Uri Uri { get; }
        public string FileName { get; }
        public string? Token { get; }
        public string? Scheme { get; }
        public Dictionary<string, IEnumerable<string>> CustomHeaders { get; }
    }
}
