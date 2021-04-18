using System.Linq;
using HotChocolate;
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
            HotChocolate.Location location,
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
            HotChocolate.Location location) =>
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

        public static void ReportError(
            this GeneratorExecutionContext context,
            IError error)
        {
            string title =
                error.Extensions is not null &&
                error.Extensions.TryGetValue(ErrorHelper.TitleExtensionKey, out var value) &&
                value is string s ? s : nameof(SourceGeneratorErrorCodes.Unexpected);

            string code = error.Code ?? SourceGeneratorErrorCodes.Unexpected;

            if (error is { Locations: { Count: > 0 } locations } &&
                error.Extensions is not null &&
                error.Extensions.TryGetValue(ErrorHelper.FileExtensionKey, out value) &&
                value is string filePath)
            {
                context.ReportDiagnostic(code, title, error.Message + error.Exception?.StackTrace, filePath, locations.First());
            }
            else
            {
                context.ReportDiagnostic(code, title, error.Message + error.Exception?.StackTrace);
            }
        }
    }
}
