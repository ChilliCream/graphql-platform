using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration;

public abstract class CodeGenerator<TDescriptor>
    : ICSharpSyntaxGenerator
    where TDescriptor : ICodeDescriptor
{
    public bool CanHandle(
        ICodeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings) =>
        descriptor is TDescriptor d && CanHandle(d, settings);

    protected virtual bool CanHandle(
        TDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings) =>
        true;

    public CSharpSyntaxGeneratorResult Generate(
        ICodeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var code = new StringBuilder();
        using var stringWriter = new StringWriter(code);
        using var codeWriter = new CodeWriter(stringWriter);

        Generate(
            (TDescriptor)descriptor,
            settings,
            codeWriter,
            out var fileName,
            out var path,
            out var ns);

        codeWriter.Flush();

        var sourceText = SourceText.From(code.ToString());
        var tree = CSharpSyntaxTree.ParseText(sourceText);

        return new CSharpSyntaxGeneratorResult(
            fileName,
            path,
            ns,
            tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().First());
    }

    protected abstract void Generate(
        TDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns);

    protected static string State => nameof(State);
    protected static string DependencyInjection => nameof(DependencyInjection);
    protected static string Serialization => nameof(Serialization);
}
