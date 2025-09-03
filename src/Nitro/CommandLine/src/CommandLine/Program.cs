using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Cloud;
using ChilliCream.Nitro.CommandLine.Fusion.Extensions;

var builder = new CommandLineBuilder(new NitroRootCommand())
    .AddNitroCloudConfiguration()
    .UseDefaults()
    .UseExceptionMiddleware()
    .UseExtendedConsole();

var (_, fusionCommand) = builder.Command.AddNitroCloudCommands();
fusionCommand.AddFusionComposeCommand();

return await builder.Build().InvokeAsync(args);
