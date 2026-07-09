using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public interface IDiagnosticsProvider
{
    ImmutableArray<Diagnostic> Diagnostics { get; }

    void AddDiagnostic(Diagnostic diagnostic);

    void AddDiagnosticRange(ImmutableArray<Diagnostic> diagnostics);
}
