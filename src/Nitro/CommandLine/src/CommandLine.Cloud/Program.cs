using System.CommandLine.Parsing;
using ChilliCream.Nitro.CLI;

return await new NitroCommandLine().Build().InvokeAsync(args);
