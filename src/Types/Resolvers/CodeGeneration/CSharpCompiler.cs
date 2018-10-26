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
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
#if RELEASE
                optimizationLevel: OptimizationLevel.Release
#else
                optimizationLevel: OptimizationLevel.Debug
#endif
                );

        public static Assembly Compile(params string[] sourceText)
        {
            if (sourceText == null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            if (sourceText.Length == 0)
            {
                throw new ArgumentException(
                    "The compiler needs at least one code unit in order " +
                    "to create an assembly.");
            }

            SyntaxTree[] syntaxTree = new SyntaxTree[sourceText.Length];
            for (int i = 0; i < sourceText.Length; i++)
            {
                syntaxTree[i] = SyntaxFactory.ParseSyntaxTree(
                    SourceText.From(sourceText[i]));
            }

            return Compile(sourceText, syntaxTree);
        }

        private static Assembly Compile(
            string[] sourceText,
            SyntaxTree[] syntaxTree)
        {
            var assemblyName = "HotChocolate.Resolvers.CodeGeneration" +
                $"._{Guid.NewGuid().ToString("N")}.dll";

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName, syntaxTree, ResolveReferences(), _options);

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
                    Environment.NewLine +
                    string.Join(Environment.NewLine, sourceText));
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
                    references.Add(MetadataReference
                        .CreateFromFile(assembly.Location));
                }
                catch
                {
                    // ignore references that cannot be added to the memory
                    // assembly. there are some unit testing assemblies that
                    // case these exceptions.
                }
            }

            return references;
        }
    }
}
