using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.Http;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class CSharpCompiler
    {
        // we pin these types so that they are added to this assembly
#pragma warning disable 414
        private static readonly EntityId? _entityId = null!;
        private static readonly JsonDocument? _jsonDocument = null!;
        private static readonly HttpConnection? _httpConnection = null!;
        private static readonly WebSocketConnection? _webSocketConnection = null!;
        private static readonly ServiceCollection? _serviceCollection = null!;
        private static readonly IHttpClientFactory? _httpClientFactory = null!;
        private static readonly HttpClient? _httpClient = null!;
#pragma warning restore 414

        private static readonly CSharpCompilationOptions _options =
            new(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);

        private static readonly HashSet<string> _excludedCodes = new()
        {
            // warning CS1702: Assuming assembly reference is of different version
            "CS1702", "CS1701"
        };

        public static IReadOnlyList<Diagnostic> GetDiagnosticErrors(params string[] sourceText)
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
            for (var i = 0; i < sourceText.Length; i++)
            {
                syntaxTree[i] = SyntaxFactory.ParseSyntaxTree(
                    SourceText.From(sourceText[i]));
            }

            var assemblyName = $"_{Guid.NewGuid():N}.dll";

            CSharpCompilation compilation = CSharpCompilation
                .Create(assemblyName, syntaxTree, Array.Empty<MetadataReference>(), _options)
                // If we load the references not twice, some assemblies are missing.
                .WithReferences(ResolveReferences());

            return compilation.GetDiagnostics()
                .Where(x =>
                    x.Severity == DiagnosticSeverity.Error && !_excludedCodes.Contains(x.Id))
                .ToList();
        }

        private static IEnumerable<MetadataReference> ResolveReferences()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(t => !t.IsDynamic && !string.IsNullOrEmpty(t.Location))
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
        }
    }
}
