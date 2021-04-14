using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StrawberryShake.CodeGeneration
{
    public class CSharpSyntaxGeneratorResult
    {
        public CSharpSyntaxGeneratorResult(
            string fileName,
            string? path,
            string ns,
            BaseTypeDeclarationSyntax typeDeclaration)
        {
            FileName = fileName;
            Path = path;
            Namespace = ns;
            TypeDeclaration = typeDeclaration;
        }

        public string FileName { get; }

        public string? Path { get; }

        public string Namespace { get; }

        public BaseTypeDeclarationSyntax TypeDeclaration { get; }
    }
}
