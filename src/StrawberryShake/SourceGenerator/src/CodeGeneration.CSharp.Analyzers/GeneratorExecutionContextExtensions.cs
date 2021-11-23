using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public static class GeneratorExecutionContextExtensions
    {
        public static void ReportDiagnostic(
            this GeneratorExecutionContext context,
            DiagnosticDescriptor descriptor,
            string filePath,
            Location location,
            params object[] messageArgs) =>
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                Microsoft.CodeAnalysis.Location.Create(
                    filePath,
                    TextSpan.FromBounds(1, 2),
                    new LinePositionSpan(
                        new LinePosition(
                            location.Line,
                            location.Column),
                        new LinePosition(
                            location.Line,
                            location.Column + 1))),
                messageArgs));

        public static void ReportDiagnostic(
            this GeneratorExecutionContext context,
            DiagnosticDescriptor descriptor,
            params object[] messageArgs) =>
            context.ReportDiagnostic(
                Diagnostic.Create(
                    descriptor,
                    Microsoft.CodeAnalysis.Location.None,
                    messageArgs));

        public static void ReportDiagnostic(
            this GeneratorExecutionContext context,
            string id,
            string title,
            string message) =>
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: id,
                        title: title,
                        messageFormat: message,
                        category: DiagnosticErrorHelper.ErrorCategory,
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Microsoft.CodeAnalysis.Location.None));

        public static void ReportDiagnostic(
            this GeneratorExecutionContext context,
            string id,
            string title,
            string message,
            string filePath,
            Location location) =>
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: id,
                        title: title,
                        messageFormat: message,
                        category: DiagnosticErrorHelper.ErrorCategory,
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Microsoft.CodeAnalysis.Location.Create(
                        filePath,
                        TextSpan.FromBounds(1, 2),
                        new LinePositionSpan(
                            new LinePosition(
                                location.Line,
                                location.Column),
                            new LinePosition(
                                location.Line,
                                location.Column + 1)))));
    }
}
