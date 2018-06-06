using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Resolvers
{
    internal static class CSharpCompiler
    {
        private static readonly CSharpCompilationOptions _options =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug);

        public static Assembly Compile(string sourceText)
        {
            File.WriteAllText(Guid.NewGuid().ToString("n") + ".cs", sourceText);

            SyntaxTree syntaxTree = ParseSource(sourceText);
            return Compile(syntaxTree);
        }

        public static SyntaxTree ParseSource(string sourceText)
        {
            return SyntaxFactory.ParseSyntaxTree(
                SourceText.From(sourceText));
        }

        private static Assembly Compile(SyntaxTree syntaxTree)
        {
            string assemblyName = "HotChocolate.Resolvers.CodeGeneration" +
                $"._{Guid.NewGuid().ToString("N")}.dll";

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName, new SyntaxTree[] { syntaxTree },
                ResolveReferences(), _options);

            using (MemoryStream stream = new MemoryStream())
            {
                EmitResult result = compilation.Emit(stream);
                if (result.Success)
                {
                    stream.Position = 0;
                    return Assembly.Load(stream.ToArray());
                }

                // TODO : EXCEPTION
                throw new Exception(string.Join(Environment.NewLine,
                    result.Diagnostics.Select(t => t.ToString())));
            }
        }

        private static IEnumerable<MetadataReference> ResolveReferences()
        {
            List<MetadataReference> references = new List<MetadataReference>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch { } // TODO : fix this
            }
            return references;
        }
    }
}
