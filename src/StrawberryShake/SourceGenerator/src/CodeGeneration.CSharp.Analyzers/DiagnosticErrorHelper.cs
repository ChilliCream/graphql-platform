using HotChocolate;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public static class DiagnosticErrorHelper
    {
        private const string _category = "StrawberryShakeGenerator";

        public static void ReportMissingDependency(
            GeneratorExecutionContext context,
            string packageName)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "SS0004",
                        title: "Dependency Missing",
                        messageFormat:
                            $"The package reference `{packageName}` is missing.\r\n" +
                            $"`dotnet add package {packageName}`",
                        category: _category,
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Microsoft.CodeAnalysis.Location.None));
        }

        public static void ReportFileError(
            GeneratorExecutionContext context,
            IError error,
            HotChocolate.Location location,
            string title,
            string code,
            string filePath)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: code,
                        title: title,
                        messageFormat: error.Message,
                        category: _category,
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Microsoft.CodeAnalysis.Location.Create(
                        filePath,
                        TextSpan.FromBounds(
                            1,
                            2),
                        new LinePositionSpan(
                            new LinePosition(
                                location.Line,
                                location.Column),
                            new LinePosition(
                                location.Line,
                                location.Column + 1)))));
        }

        public static void ReportGeneralError(
            GeneratorExecutionContext context,
            IError error,
            string title,
            string code)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: code,
                        title: title,
                        messageFormat: $"An error occurred during generation: {error.Message}",
                        category: _category,
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        description: error.Message),
                    Microsoft.CodeAnalysis.Location.None));
        }
    }
}
