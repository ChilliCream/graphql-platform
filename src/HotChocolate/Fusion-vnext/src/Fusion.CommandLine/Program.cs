using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using HotChocolate.Fusion.CommandLine;

return await new CommandLineBuilder(new FusionRootCommand())
    .AddFusion()
    .UseDefaults()
    .Build()
    .InvokeAsync(args);
