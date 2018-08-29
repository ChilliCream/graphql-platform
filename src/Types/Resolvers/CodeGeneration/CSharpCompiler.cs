using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            SyntaxTree syntaxTree = ParseSource(sourceText);
            return Compile(sourceText, syntaxTree);
        }

        public static SyntaxTree ParseSource(string sourceText)
        {
            return SyntaxFactory.ParseSyntaxTree(
                SourceText.From(sourceText));
        }

        private static Assembly Compile(string sourceText, SyntaxTree syntaxTree)
        {
            var assemblyName = "HotChocolate.Resolvers.CodeGeneration" +
                $"._{Guid.NewGuid().ToString("N")}.dll";

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName, new SyntaxTree[] { syntaxTree },
                ResolveReferences(), _options);

            using (var stream = new MemoryStream())
            {
                EmitResult result = compilation.Emit(stream);
                if (result.Success)
                {
                    stream.Position = 0;
                    return Assembly.Load(stream.ToArray());
                }

                // TODO : EXCEPTION
                throw new Exception(string.Join(Environment.NewLine,
                    result.Diagnostics.Select(t => t.ToString())) +
                    Environment.NewLine + sourceText);
            }
        }

        private static IEnumerable<MetadataReference> ResolveReferences()
        {
            var references = new List<MetadataReference>();
            foreach (Assembly assembly in AppDomain.CurrentDomain
                .GetAssemblies().Where(t => !t.IsDynamic))
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch
                {


                } // TODO : fix this
            }
            return references;
        }
    }
}
