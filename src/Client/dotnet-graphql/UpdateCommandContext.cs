using System;

namespace StrawberryShake.Tools
{
    public class UpdateCommandContext
    {
        public UpdateCommandContext(
            Uri? uri,
            string? path,
            string? token,
            string scheme)
        {
            Uri = uri;
            Path = path;
            Token = token;
            Scheme = scheme;
        }

        public Uri? Uri { get; }
        public string? Path { get; }
        public string? Token { get; }
        public string Scheme { get; }
    }
}
