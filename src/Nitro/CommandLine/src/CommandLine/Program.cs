using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Cloud;
using HotChocolate.Fusion.CommandLine;

var builder = new CommandLineBuilder(new NitroRootCommand())
    .AddNitroCloudConfiguration()
    .UseDefaults()
    .UseExceptionMiddleware()
    .UseExtendedConsole();

var (_, fusionCommand) = builder.Command.AddNitroCloudCommands();
fusionCommand.AddFusionComposeCommand();

return await builder.Build().InvokeAsync(args);
