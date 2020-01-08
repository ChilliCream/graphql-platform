using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public static class PublishSchemaCommand
    {
        public static CommandLineApplication Create()
        {
            var init = new CommandLineApplication();
            init.AddName("download");
            init.AddHelp<InitHelpTextGenerator>();

            CommandArgument uriArg = init.Argument(
                "uri",
                "The URL to the GraphQL endpoint.",
                c => c.IsRequired());

            CommandOption fileNameArg = init.Option(
                "-f|--FileName",
                "The file name to store the schema SDL.",
                CommandOptionType.SingleValue);

            CommandOption tokenEndpointArg = init.Option(
                "--tokenEndpoint",
                "The token endpoint uri.",
                CommandOptionType.SingleValue);

            CommandOption clientId = init.Option(
                "--clientId",
                "The client id.",
                CommandOptionType.SingleValue);

            CommandOption clientSecret = init.Option(
                "--clientSecret",
                "The client secret.",
                CommandOptionType.SingleValue);

            CommandOption scopes = init.Option(
                "--scope",
                "A custom scope that shall be passed along with the token request.",
                CommandOptionType.MultipleValue);

            CommandOption tokenArg = init.Option(
                "-t|--token",
                "The token that shall be used to authenticate with the GraphQL server.",
                CommandOptionType.SingleValue);

            CommandOption schemeArg = init.Option(
                "-s|--scheme",
                "The token scheme (default: bearer).",
                CommandOptionType.SingleValue);

            CommandOption jsonArg = init.Option(
                "-j|--json",
                "Console output as JSON.",
                CommandOptionType.NoValue);

            init.OnExecuteAsync(cancellationToken =>
            {
                var arguments = new PublishSchemaCommandArguments(
                    uriArg,
                    fileNameArg,
                    tokenArg,
                    schemeArg,
                    tokenEndpointArg,
                    clientId,
                    clientSecret,
                    scopes);
                var handler = CommandTools.CreateHandler<PublishSchemaCommandHandler>(jsonArg);
                return handler.ExecuteAsync(arguments, cancellationToken);
            });

            return init;
        }
    }
}
