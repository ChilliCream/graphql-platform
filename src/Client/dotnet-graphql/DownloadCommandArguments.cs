using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public class DownloadCommandArguments
    {
        public DownloadCommandArguments(
            CommandArgument uri,
            CommandOption fileName,
            CommandOption token,
            CommandOption scheme)
        {
            Uri = uri;
            FileName = fileName;
            Token = token;
            Scheme = scheme;
        }

        public CommandArgument Uri { get; }
        public CommandOption FileName { get; }
        public CommandOption Token { get; }
        public CommandOption Scheme { get; }
    }
}
