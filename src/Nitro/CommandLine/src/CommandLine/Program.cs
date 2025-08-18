using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using ChilliCream.Nitro.CommandLine;
using ChilliCream.Nitro.CommandLine.Cloud;
using HotChocolate.Fusion.CommandLine;

return await new CommandLineBuilder(new NitroRootCommand())
    .AddFusion()
    .AddNitroCloud()
    .UseDefaults()
    .UseExceptionMiddleware()
    .UseExtendedConsole()
    .Build()
    .InvokeAsync(args);
