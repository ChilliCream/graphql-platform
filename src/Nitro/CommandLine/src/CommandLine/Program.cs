using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using ChilliCream.Nitro;
using ChilliCream.Nitro.CLI;
using ChilliCream.Nitro.CommandLine;

return await new CommandLineBuilder(new NitroRootCommand())
    .AddNitroCloud()
    .UseDefaults()
    .UseExceptionMiddleware()
    .UseExtendedConsole()
    .Build()
    .InvokeAsync(args);
