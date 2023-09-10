using McMaster.Extensions.CommandLineUtils;
using StrawberryShake.Tools.OAuth;

namespace StrawberryShake.Tools;

public class UpdateCommandArguments
{
    public UpdateCommandArguments(
        CommandOption uri,
        CommandOption path,
        AuthArguments authArguments,
        CommandOption customHeaders)
    {
        Uri = uri;
        Path = path;
        AuthArguments = authArguments;
        CustomHeaders = customHeaders;
    }

    public CommandOption Uri { get; }

    public CommandOption Path { get; }

    public AuthArguments AuthArguments { get; }

    public CommandOption CustomHeaders { get; }
}