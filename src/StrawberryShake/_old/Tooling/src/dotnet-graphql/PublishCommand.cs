using McMaster.Extensions.CommandLineUtils;

namespace StrawberryShake.Tools
{
    public static class PublishCommand
    {
        public static CommandLineApplication Create()
        {
            var publish = new CommandLineApplication();
            publish.AddName("publish");
            publish.AddSubcommand(PublishSchemaCommand.Create());
            publish.AddSubcommand(PublishClientCommand.Create());
            return publish;
        }
    }
}
