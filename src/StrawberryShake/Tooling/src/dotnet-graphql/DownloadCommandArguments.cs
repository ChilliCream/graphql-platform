using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools;

public class DownloadCommandArguments
{
    public DownloadCommandArguments(
        CommandArgument uri,
        CommandOption fileName,
        AuthArguments authArguments,
        CommandOption customHeaders,
        CommandOption typeDepth)
    {
        Uri = uri;
        FileName = fileName;
        AuthArguments = authArguments;
        CustomHeaders = customHeaders;
        TypeDepth = typeDepth;
    }

    public CommandArgument Uri { get; }
    public CommandOption FileName { get; }
    public AuthArguments AuthArguments { get; }
    public CommandOption CustomHeaders { get; }
    public CommandOption TypeDepth { get; }
}
