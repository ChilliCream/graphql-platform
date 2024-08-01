using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools;

public class InitCommandArguments
{
    public InitCommandArguments(
        CommandArgument uri,
        CommandOption path,
        CommandOption name,
        AuthArguments authArguments,
        CommandOption customHeaders,
        CommandOption fromFile,
        CommandOption typeDepth)
    {
        Uri = uri;
        Path = path;
        Name = name;
        AuthArguments = authArguments;
        CustomHeaders = customHeaders;
        FromFile = fromFile;
        TypeDepth = typeDepth;
    }

    public CommandArgument Uri { get; }
    public CommandOption Path { get; }
    public CommandOption Name { get; }
    public AuthArguments AuthArguments { get; }
    public CommandOption CustomHeaders { get; }
    public CommandOption FromFile { get; }
    public CommandOption TypeDepth { get; }
}
