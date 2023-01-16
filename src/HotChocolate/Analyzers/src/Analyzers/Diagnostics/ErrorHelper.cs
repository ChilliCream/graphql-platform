using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Analyzers.Diagnostics
{
    public static class ErrorHelper
    {
        public const string File = "file";
        public const string Title = "title";
        public const string ErrorCategory = "HotChocolateSourceGenerator";

#pragma warning disable RS2008
#pragma warning disable RS1032
        private static readonly DiagnosticDescriptor _missingDependency =
            new DiagnosticDescriptor(
                id: ErrorCodes.DependencyMissing,
                title: "Dependency Missing",
                messageFormat: "The package reference `{0}` is missing. `dotnet add package {0}`",
                category: ErrorCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
#pragma warning restore RS1032
#pragma warning restore RS2008

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
                        category: ErrorCategory,
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
                        category: ErrorCategory,
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
            var title =
                error.Extensions is not null &&
                error.Extensions.TryGetValue(ErrorHelper.Title, out var value) &&
                value is string s ? s : nameof(ErrorCodes.Unexpected);

            var code = error.Code ?? ErrorCodes.Unexpected;

            if (error is { Locations: { Count: > 0 } locations } &&
                error.Extensions is not null &&
                error.Extensions.TryGetValue(ErrorHelper.File, out value) &&
                value is string filePath)
            {
                context.ReportDiagnostic(code, title, error.Message, filePath, locations.First());
            }
            else
            {
                context.ReportDiagnostic(code, title, error.Message);
            }
        }

        public static void ReportError(
            this GeneratorExecutionContext context,
            Exception exception) =>
            ReportError(
                context,
                ErrorBuilder.New()
                    .SetMessage(exception.Message + Environment.NewLine + exception.GetType().Name)
                    .SetException(exception)
                    .Build());

        public static void ReportMissingDependency(
            GeneratorExecutionContext context,
            string packageName) =>
            context.ReportDiagnostic(_missingDependency, packageName);
    }
}
