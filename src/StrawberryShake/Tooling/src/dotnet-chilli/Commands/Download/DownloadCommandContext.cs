using System;

namespace StrawberryShake.Tools.Commands.Download
{
    public class DownloadCommandContext
    {
        public DownloadCommandContext(
            Uri uri,
            string fileName,
            string? token,
            string? scheme)
        {
            Uri = uri;
            FileName = fileName;
            Token = token;
            Scheme = scheme;
        }

        public Uri Uri { get; }
        public string FileName { get; }
        public string? Token { get; }
        public string? Scheme { get; }
    }
}
