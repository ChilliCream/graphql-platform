using System.CommandLine.Builder;
using System.CommandLine.Parsing;

var builder = new CommandLineBuilder(new NitroRootCommand())
    .AddNitroCloudConfiguration()
    .UseDefaults()
    .UseExceptionMiddleware()
    .UseExtendedConsole();

builder.Command.AddNitroCloudCommands();

return await builder.Build().InvokeAsync(args);
