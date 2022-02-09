using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars;

public static class DependencyInjection
{
    public static void Configure(IServiceCollection services)
    {
        services.AddStarWarsClient();
    }
}

/*
public static class Utilities
{
    private static string? GetGeneratedOutput(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(assembly => !assembly.IsDynamic)
                                  .Select(assembly => MetadataReference
                                                      .CreateFromFile(assembly.Location))
                                  .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create("SourceGeneratorTests",
                      new[] { syntaxTree },
                      references,
                      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Source Generator to test
        var generator = new DemoSourceGenerator.DemoSourceGenerator();

        CSharpGeneratorDriver.Create(generator)
                             .RunGeneratorsAndUpdateCompilation(compilation,
                                                                out var outputCompilation,
                                                                out var diagnostics);

        // optional
        diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                   .Should().BeEmpty();

        return outputCompilation.SyntaxTrees.Skip(1).LastOrDefault()?.ToString();
    }
}
*/
