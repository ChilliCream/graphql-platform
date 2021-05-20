using Microsoft.CodeAnalysis;
using Location = HotChocolate.Location;
using static StrawberryShake.CodeGeneration.CSharp.Analyzers.Properties.AnalyzerResources;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public static class DiagnosticErrorHelper
    {
        public const string ErrorCategory = "StrawberryShakeGenerator";

        private const string _missingDependencyCode = "SS0004";

        private const string _invalidClientNameCode = "SS0014";

        private static readonly DiagnosticDescriptor _missingDependency =
            new DiagnosticDescriptor(
                id: _missingDependencyCode,
                title: DiagnosticErrorHelper_ReportMissingDependency_Title,
                messageFormat: DiagnosticErrorHelper_ReportMissingDependency_Message,
                category: ErrorCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor _invalidClientName =
            new DiagnosticDescriptor(
                id: _invalidClientNameCode,
                title: DiagnosticErrorHelper_ReportMissingDependency_Title,
                messageFormat: DiagnosticErrorHelper_ReportInvalidClientName_Message,
                category: ErrorCategory,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static void ReportMissingDependency(
            GeneratorExecutionContext context,
            string packageName) =>
            context.ReportDiagnostic(_missingDependency, packageName);

        public static void ReportInvalidClientName(
            GeneratorExecutionContext context,
            string clientName,
            string filePath) =>
            context.ReportDiagnostic(_invalidClientName, clientName, filePath, new Location(1, 1));
    }
}
