using HotChocolate.Types.Analyzers.Properties;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers;

public static class Errors
{
    public static readonly DiagnosticDescriptor KeyParameterMissing =
        new(
            id: "HC0074",
            title: "Parameter Missing.",
            messageFormat: SourceGenResources.DataLoader_KeyParameterMissing,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodAccessModifierInvalid =
        new(
            id: "HC0075",
            title: "Access Modifier Invalid.",
            messageFormat: SourceGenResources.DataLoader_InvalidAccessModifier,
            category: "DataLoader",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ObjectTypePartialKeywordMissing =
        new(
            id: "HC00XX",
            title: "Partial Keyword Missing.",
            messageFormat: "A split object type class needs to be a partial class.",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ObjectTypeStaticKeywordMissing =
        new(
            id: "HC00XX",
            title: "Static Keyword Missing.",
            messageFormat: "A split object type class needs to be a static class.",
            category: "TypeSystem",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
