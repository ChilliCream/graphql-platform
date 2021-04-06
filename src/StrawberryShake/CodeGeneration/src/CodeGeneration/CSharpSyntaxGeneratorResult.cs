using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StrawberryShake.CodeGeneration
{
    public class CSharpSyntaxGeneratorResult
    {
        public CSharpSyntaxGeneratorResult(
            string fileName,
            string? path,
            string ns,
            TypeDeclarationSyntax typeDeclaration)
        {
            FileName = fileName;
            Path = path;
            Namespace = ns;
            TypeDeclaration = typeDeclaration;
        }

        public string FileName { get; }

        public string? Path { get; }

        public string Namespace { get; }

        public TypeDeclarationSyntax TypeDeclaration { get; }
    }
}
