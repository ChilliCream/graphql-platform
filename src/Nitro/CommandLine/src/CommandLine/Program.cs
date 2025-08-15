using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using ChilliCream.Nitro;
using ChilliCream.Nitro.CLI;
using ChilliCream.Nitro.CommandLine;
using HotChocolate.Fusion.CommandLine;

return await new CommandLineBuilder(new NitroRootCommand())
    .AddFusion()
    .AddNitroCloud()
    .UseDefaults()
    .UseExceptionMiddleware()
    .UseExtendedConsole()
    .Build()
    .InvokeAsync(args);
