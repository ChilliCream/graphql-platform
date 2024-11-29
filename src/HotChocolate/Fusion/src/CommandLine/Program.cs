using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.CommandLine;

public static class Program
{
    [RequiresUnreferencedCode("HotChocolate.Fusion is not trim compatible.")]
    public static async Task<int> Main(string[] args)
    {
        var builder = App.CreateBuilder();

        var app = builder.Build();

        return await app.InvokeAsync(args);
    }
}
