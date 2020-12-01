using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools
{
    public class DownloadCommandArguments
    {
        public DownloadCommandArguments(
            CommandArgument uri,
            CommandOption fileName,
            AuthArguments authArguments)
        {
            Uri = uri;
            FileName = fileName;
            AuthArguments = authArguments;
        }

        public CommandArgument Uri { get; }
        public CommandOption FileName { get; }
        public AuthArguments AuthArguments { get; }
    }
}
