using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Linq;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.SymbolDisplayFormat;
using static Microsoft.CodeAnalysis.SymbolDisplayMiscellaneousOptions;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat s_format =
        FullyQualifiedFormat.AddMiscellaneousOptions(
            IncludeNullableReferenceTypeModifier);

    public static string GetName(this ISymbol symbol)
    {
        var name = GetNameFromAttribute(symbol);

        if (string.IsNullOrEmpty(name))
        {
            name = symbol.Name;
        }

        return name!;
    }

    public static MethodDescription GetDescription(this IMethodSymbol method)
        => method.GetDescription(null);

    public static MethodDescription GetDescription(this IMethodSymbol method, Compilation? compilation)
    {
        var methodDescription = GetDescriptionFromAttribute(method);

        if (methodDescription == null && compilation != null)
        {
            // Try inheritance-aware resolution with Compilation
            methodDescription = GetSummaryDocumentationWithInheritance(method, compilation);
        }
        else if (methodDescription == null)
        {
            // Fallback to simple XML extraction without inheritdoc support
            var xml = method.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xml))
            {
                try
                {
                    var doc = XDocument.Parse(xml);
                    var summaryText = doc.Descendants("summary")
                        .FirstOrDefault()?
                        .Value;

                    methodDescription = GeneratorUtils.NormalizeXmlDocumentation(summaryText);
                }
                catch
                {
                    // XML documentation parsing is best-effort only.
                    // Malformed XML is ignored and we fall back to no description.
                }
            }
        }

        // Process parameter descriptions
        var parameters = method.Parameters;
        var paramDescriptions = ImmutableArray.CreateBuilder<string?>(parameters.Length);

        foreach (var param in parameters)
        {
            var paramDescription = GetDescriptionFromAttribute(param);
            var commentXml = method.GetDocumentationCommentXml();

            if (paramDescription == null && !string.IsNullOrEmpty(commentXml))
            {
                try
                {
                    var doc = XDocument.Parse(commentXml);
                    var paramDoc = doc.Descendants("param")
                        .FirstOrDefault(p => p.Attribute("name")?.Value == param.Name)?
                        .Value;

                    paramDescription = GeneratorUtils.NormalizeXmlDocumentation(paramDoc);
                }
                catch
                {
                    // XML documentation parsing is best-effort only.
                    // Malformed XML is ignored and we fall back to no description.
                }
            }

            paramDescriptions.Add(paramDescription);
        }

        return new MethodDescription(methodDescription, paramDescriptions.ToImmutable());
    }

    public static PropertyDescription? GetDescription(this IPropertySymbol property)
        => property.GetDescription(null);

    public static PropertyDescription? GetDescription(this IPropertySymbol property, Compilation? compilation)
    {
        var description = GetDescriptionFromAttribute(property);
        if (description != null)
        {
            return new PropertyDescription(description);
        }

        if (compilation != null)
        {
            // Try inheritance-aware resolution with Compilation
            return new PropertyDescription(GetSummaryDocumentationWithInheritance(property, compilation));
        }

        // Fallback to simple XML extraction without inheritdoc support
        var commentXml = property.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(commentXml))
        {
            return null;
        }

        try
        {
            var doc = XDocument.Parse(commentXml);
            var summaryElement = doc.Descendants("summary").FirstOrDefault();
            var text = summaryElement?.Value;
            return new PropertyDescription(GeneratorUtils.NormalizeXmlDocumentation(text));
        }
        catch
        {
            // XML documentation parsing is best-effort only.
            // Malformed XML is ignored and we fall back to no description.
            return null;
        }
    }

    public static string? GetDescription(this INamedTypeSymbol type)
        => type.GetDescription(null);

    public static string? GetDescription(this INamedTypeSymbol type, Compilation? compilation)
    {
        var description = GetDescriptionFromAttribute(type);
        if (description != null)
        {
            return description;
        }

        if (compilation != null)
        {
            // Try inheritance-aware resolution with Compilation
            return GetSummaryDocumentationWithInheritance(type, compilation);
        }

        // Fallback to simple XML extraction without inheritdoc support
        var xml = type.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(xml))
        {
            return null;
        }

        try
        {
            var doc = XDocument.Parse(xml);
            var summaryElement = doc.Descendants("summary").FirstOrDefault();
            var text = summaryElement?.Value;
            return GeneratorUtils.NormalizeXmlDocumentation(text);
        }
        catch
        {
            // XML documentation parsing is best-effort only.
            // Malformed XML is ignored and we fall back to no description.
            return null;
        }
    }

    public static string? GetDescriptionFromAttribute(this ISymbol symbol)
    {
        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "GraphQLDescriptionAttribute");

        if (attribute?.ConstructorArguments.Length > 0)
        {
            var value = attribute.ConstructorArguments[0].Value as string;
            return string.IsNullOrEmpty(value) ? null : value;
        }

        return null;
    }

    private static string? GetNameFromAttribute(ISymbol symbol)
    {
        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "GraphQLNameAttribute");

        if (attribute?.ConstructorArguments.Length > 0)
        {
            var value = attribute.ConstructorArguments[0].Value as string;
            return string.IsNullOrEmpty(value) ? null : value;
        }

        return null;
    }

    /// <summary>
    /// Extracts summary text from XML documentation, resolving tags with semantic relevance (f. e. inheritdoc or see).
    /// </summary>
    private static string? GetSummaryDocumentationWithInheritance(ISymbol symbol, Compilation compilation)
    {
        var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        return GetSummaryDocumentationWithInheritanceCore(symbol, compilation, visited);
    }

    /// <summary>
    /// Core implementation with cycle detection.
    /// </summary>
    private static string? GetSummaryDocumentationWithInheritanceCore(
        ISymbol symbol,
        Compilation compilation,
        HashSet<ISymbol> visited)
    {
        // Prevent infinite recursion
        if (!visited.Add(symbol))
        {
            return null;
        }

        var xml = GetXmlDocumentationFromSyntax(symbol);
        if (string.IsNullOrEmpty(xml))
        {
            return null;
        }

        try
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);

            // Materialize relevant XML elements (-> replace their element with the actual textual representation)
            MaterializeInheritdocElements(doc);
            MaterializeSeeElements(doc);
            MaterializeParamRefElements(doc);

            var summaryText =
                doc.Descendants("summary").FirstOrDefault()?.Value ??
                doc.Descendants("member").FirstOrDefault()?.Value;

            summaryText += GetReturnsElementText(doc);

            var exceptionDoc = GetExceptionDocumentation(doc);
            if (!string.IsNullOrEmpty(exceptionDoc))
            {
                summaryText += "\n\n**Errors:**\n" + exceptionDoc;
            }

            return GeneratorUtils.NormalizeXmlDocumentation(summaryText);
        }
        catch
        {
            // XML documentation parsing is best-effort only.
            return null;
        }

        void MaterializeInheritdocElements(XDocument doc1)
        {
            foreach (var inheritdocElement in doc1.Descendants("inheritdoc").ToArray())
            {
                if (inheritdocElement == null)
                {
                    continue;
                }

                // Try to resolve the inherited documentation
                var inheritedDoc = ResolveInheritdoc(symbol, inheritdocElement, compilation, visited);
                if (inheritedDoc != null)
                {
                    inheritdocElement.ReplaceWith(inheritedDoc);
                }
            }
        }

        static void MaterializeSeeElements(XDocument xDocument)
        {
            foreach (var seeElement in xDocument.Descendants("see").ToArray())
            {
                if (seeElement == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(seeElement.Value))
                {
                    seeElement.ReplaceWith(seeElement.Value);
                    continue;
                }

                var attribute = seeElement.Attribute("langword") ?? seeElement.Attribute("href");
                if (attribute != null)
                {
                    seeElement.ReplaceWith(attribute.Value);
                    continue;
                }

                attribute = seeElement.Attribute("cref");
                if (attribute?.Value != null)
                {
                    var index = attribute.Value.LastIndexOf('.');
                    seeElement.ReplaceWith(attribute.Value.Substring(index + 1));
                }
            }
        }

        static void MaterializeParamRefElements(XDocument xDocument)
        {
            foreach (var paramref in xDocument.Descendants("paramref").ToArray())
            {
                var attribute = paramref?.Attribute("name");
                if (attribute != null)
                {
                    paramref!.ReplaceWith(attribute.Value);
                }
            }
        }

        static string GetExceptionDocumentation(XDocument xDocument)
        {
            StringBuilder? builder = null;
            var errorCount = 0;
            var exceptionElements = xDocument.Descendants("exception");
            foreach (var exceptionElement in exceptionElements)
            {
                if (string.IsNullOrEmpty(exceptionElement.Value))
                {
                    continue;
                }

                var code = exceptionElement.Attribute("code");
                if (string.IsNullOrEmpty(code?.Value))
                {
                    continue;
                }

                builder ??= new StringBuilder();
                builder.Append(builder.Length > 0 ? "\n" : string.Empty)
                    .Append(++errorCount)
                    .Append('.')
                    .Append(' ')
                    .Append(code!.Value)
                    .Append(':')
                    .Append(' ')
                    .Append(exceptionElement.Value);
            }

            return builder?.ToString() ?? string.Empty;
        }

        static string GetReturnsElementText(XDocument doc)
        {
            var xElement = doc.Descendants("returns").FirstOrDefault();
            if (xElement?.Value != null)
            {
                return "\n\n**Returns:**\n" + xElement.Value;
            }

            return string.Empty;
        }
    }

    private static string? GetXmlDocumentationFromSyntax(ISymbol symbol)
    {
        // Note: One currently can't use GetDocumentationCommentXml in source generators for any other assembly than the SG-assembly itself.
        // See https://github.com/dotnet/roslyn/issues/23673 and https://github.com/dotnet/roslyn/issues/23673#issuecomment-2108664480
        // "Syntax is not available for symbols defined outside the current project (including cases where the symbol is defined in a different project in the same solution)"
        var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        while (syntax is VariableDeclaratorSyntax vds)
        {
            syntax = vds.Parent?.Parent;
        }

        if (syntax == null || syntax.SyntaxTree.Options.DocumentationMode == DocumentationMode.None)
        {
            // See https://github.com/dotnet/roslyn/issues/58210, for DocumentationMode.None we can`t reliably extract the xml doc
            // It is possible with heuristics, tough (inspecting Mulit-/SingleLineCommentTrivia)
            return null;
        }

        var trivia = syntax.GetLeadingTrivia();
        StringBuilder? builder = null;
        foreach (var comment in trivia)
        {
            if (comment.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || comment.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                var stringComment = comment.ToString();
                foreach (var s in stringComment.Split('\n'))
                {
                    builder ??= new StringBuilder();
                    builder.Append(s.TrimStart().Replace("///", string.Empty));
                    builder.Append('\n');
                }
            }
        }

        return builder?
            .Insert(0, "<member>")
            .Append("</member>")
            .ToString();
    }

    /// <summary>
    /// Resolves an inheritdoc element by finding the referenced member.
    /// </summary>
    private static string? ResolveInheritdoc(
        ISymbol symbol,
        XElement inheritdocElement,
        Compilation compilation,
        HashSet<ISymbol> visited)
    {
        // Check for cref attribute (explicit reference)
        var crefAttr = inheritdocElement.Attribute("cref");
        if (crefAttr != null)
        {
            var referencedSymbol = ResolveDocumentationId(crefAttr.Value, compilation, symbol);
            if (referencedSymbol != null)
            {
                return GetSummaryDocumentationWithInheritanceCore(referencedSymbol, compilation, visited);
            }
            return null;
        }

        // No cref - resolve from base class or interface
        var baseMember = FindBaseMember(symbol);
        if (baseMember != null)
        {
            return GetSummaryDocumentationWithInheritanceCore(baseMember, compilation, visited);
        }

        return null;
    }

    /// <summary>
    /// Finds the base member (from base class or interface) that this symbol overrides or implements.
    /// </summary>
    private static ISymbol? FindBaseMember(ISymbol symbol)
    {
        // Check method override
        if (symbol is IMethodSymbol method)
        {
            if (method.OverriddenMethod != null)
            {
                return method.OverriddenMethod;
            }

            // Check interface implementation
            var interfaceMember = FindInterfaceMember(method);
            if (interfaceMember != null)
            {
                return interfaceMember;
            }
        }

        // Check property override
        if (symbol is IPropertySymbol property)
        {
            if (property.OverriddenProperty != null)
            {
                return property.OverriddenProperty;
            }

            // Check interface implementation
            var interfaceMember = FindInterfaceMember(property);
            if (interfaceMember != null)
            {
                return interfaceMember;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the interface member that this method implements.
    /// </summary>
    private static IMethodSymbol? FindInterfaceMember(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType == null)
        {
            return null;
        }

        foreach (var @interface in containingType.AllInterfaces)
        {
            foreach (var member in @interface.GetMembers())
            {
                if (member is IMethodSymbol interfaceMethod
                    && interfaceMethod.Name == method.Name
                    && method.Equals(containingType.FindImplementationForInterfaceMember(interfaceMethod), SymbolEqualityComparer.Default))
                {
                    return interfaceMethod;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the interface member that this property implements.
    /// </summary>
    private static IPropertySymbol? FindInterfaceMember(IPropertySymbol property)
    {
        var containingType = property.ContainingType;
        if (containingType == null)
        {
            return null;
        }

        foreach (var @interface in containingType.AllInterfaces)
        {
            foreach (var member in @interface.GetMembers())
            {
                if (member is IPropertySymbol interfaceProperty
                    && interfaceProperty.Name == property.Name
                    && property.Equals(containingType.FindImplementationForInterfaceMember(interfaceProperty), SymbolEqualityComparer.Default))
                {
                    return interfaceProperty;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves a documentation ID (cref value) to a symbol.
    /// Handles format like "T:Namespace.Type", "M:Namespace.Type.Method", "T:Namespace.Type`1", etc.
    /// </summary>
    private static ISymbol? ResolveDocumentationId(string documentationId, Compilation compilation, ISymbol contextSymbol)
    {
        if (string.IsNullOrEmpty(documentationId))
        {
            return null;
        }

        if (documentationId.Length > 1 && documentationId[1] == ':')
        {
            documentationId = documentationId.Substring(2);
        }

        var result = compilation.GetTypeByMetadataName(documentationId) ??
            ResolveMemberSymbol(documentationId, compilation) ??
            ResolveMethodSymbol(documentationId, compilation);

        var @namespace = contextSymbol.ContainingNamespace?.ToString();
        if (result == null && !string.IsNullOrEmpty(@namespace) && !documentationId.StartsWith(@namespace))
        {
            documentationId = @namespace + "." + documentationId;
            result = compilation.GetTypeByMetadataName(documentationId) ??
                ResolveMemberSymbol(documentationId, compilation) ??
                ResolveMethodSymbol(documentationId, compilation);
        }

        return result;
    }

    private static ISymbol? ResolveMethodSymbol(string documentationId, Compilation compilation)
    {
        if (string.IsNullOrEmpty(documentationId))
        {
            return null;
        }

        var openParenthesisIndex = documentationId.LastIndexOf('(');
        var qualifiedName = openParenthesisIndex >= 0
            ? documentationId.Substring(0, openParenthesisIndex)
            : documentationId;

        var lastDotIndex = qualifiedName.LastIndexOf('.');
        if (lastDotIndex < 0)
        {
            return null;
        }

        var typeName = qualifiedName.Substring(0, lastDotIndex);
        var methodName = qualifiedName.Substring(lastDotIndex + 1);

        var typeSymbol = ResolveTypeSymbol(typeName, compilation);
        if (typeSymbol == null)
        {
            return null;
        }

        return typeSymbol
            .GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.ToString() == documentationId);
    }

    private static ISymbol? ResolveMemberSymbol(string documentationId, Compilation compilation)
    {
        var lastDotIndex = documentationId.LastIndexOf('.');
        if (lastDotIndex < 0)
        {
            return null;
        }

        var typeName = documentationId.Substring(0, lastDotIndex);
        var memberName = documentationId.Substring(lastDotIndex + 1);

        var typeSymbol = ResolveTypeSymbol(typeName, compilation);
        return typeSymbol?.GetMembers(memberName).FirstOrDefault();
    }

    private static INamedTypeSymbol? ResolveTypeSymbol(string typeName, Compilation compilation)
    {
        // Non-nested type
        var symbol = compilation.GetTypeByMetadataName(typeName);
        if (symbol != null)
        {
            return symbol;
        }

        // Nested type
        var nestedName = typeName;
        while (true)
        {
            var lastDot = nestedName.LastIndexOf('.');
            if (lastDot < 0)
            {
                return null;
            }

            nestedName = nestedName.Remove(lastDot, 1).Insert(lastDot, "+");
            symbol = compilation.GetTypeByMetadataName(nestedName);
            if (symbol != null)
            {
                return symbol;
            }
        }
    }

    public static bool IsNullableType(this ITypeSymbol typeSymbol)
        => typeSymbol.IsNullableRefType() || typeSymbol.IsNullableValueType();

    public static bool IsNullableRefType(this ITypeSymbol typeSymbol)
        => typeSymbol is
        {
            IsReferenceType: true,
            NullableAnnotation: NullableAnnotation.Annotated
        };

    public static bool IsNullableValueType(this ITypeSymbol typeSymbol)
        => typeSymbol is INamedTypeSymbol
        {
            IsGenericType: true,
            OriginalDefinition.SpecialType: SpecialType.System_Nullable_T
        };

    public static string PrintNullRefQualifier(this ITypeSymbol typeSymbol)
        => typeSymbol.IsNullableRefType() ? "?" : string.Empty;

    public static string ToFullyQualified(this ITypeSymbol typeSymbol)
        => typeSymbol.ToDisplayString(FullyQualifiedFormat);

    public static string ToFullyQualifiedWithNullRefQualifier(this ITypeSymbol typeSymbol)
        => typeSymbol.ToDisplayString(s_format);

    public static string ToNullableFullyQualifiedWithNullRefQualifier(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsValueType)
        {
            return typeSymbol.ToFullyQualifiedWithNullRefQualifier();
        }

        var value = typeSymbol.ToFullyQualifiedWithNullRefQualifier();
        return value.Length > 0 && value[value.Length - 1] != '?' ? value + "?" : value;
    }

    public static string ToClassNonNullableFullyQualifiedWithNullRefQualifier(this ITypeSymbol typeSymbol)
    {
        var value = typeSymbol.ToFullyQualifiedWithNullRefQualifier();
        return !typeSymbol.IsValueType && value.Length > 0 && value[value.Length - 1] == '?'
            ? value.Substring(0, value.Length - 1)
            : value;
    }

    public static bool IsParent(this IParameterSymbol parameter)
        => parameter.IsThis
            || parameter
                .GetAttributes()
                .Any(static t => t.AttributeClass?.ToDisplayString() == WellKnownAttributes.ParentAttribute);

    public static bool IsIgnored(this ISymbol member)
        => member.GetAttributes()
            .Any(static t => t.AttributeClass?.ToDisplayString() == WellKnownAttributes.GraphQLIgnoreAttribute);

    public static bool IsCancellationToken(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.CancellationToken;

    public static bool IsClaimsPrincipal(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.ClaimsPrincipal;

    public static bool IsDocumentNode(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.DocumentNode;

    public static bool IsFieldNode(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.FieldNode;

    public static bool IsOutputField(this IParameterSymbol parameterSymbol, Compilation compilation)
    {
        var type = compilation.GetTypeByMetadataName(WellKnownTypes.OutputField);
        return type != null && compilation.ClassifyConversion(parameterSymbol.Type, type).IsImplicit;
    }

    public static bool IsHttpContext(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.HttpContext;

    public static bool IsHttpRequest(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.HttpRequest;

    public static bool IsHttpResponse(this IParameterSymbol parameter)
        => parameter.Type.ToDisplayString() == WellKnownTypes.HttpResponse;

    public static bool IsSetState(this IParameterSymbol parameter, [NotNullWhen(true)] out string? stateTypeName)
    {
        if (parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 }
            && namedTypeSymbol.Name == "SetState"
            && namedTypeSymbol.ContainingNamespace.ToDisplayString() == "HotChocolate")
        {
            stateTypeName = namedTypeSymbol.TypeArguments[0].ToDisplayString();
            return true;
        }

        stateTypeName = null;
        return false;
    }

    public static bool IsSetState(this IParameterSymbol parameter)
        => parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 }
            && namedTypeSymbol.Name == "SetState"
            && namedTypeSymbol.ContainingNamespace.ToDisplayString() == "HotChocolate";

    public static bool IsQueryContext(this IParameterSymbol parameter)
        => parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 }
            && namedTypeSymbol.ToDisplayString().StartsWith(WellKnownTypes.QueryContextGeneric);

    public static bool IsPagingArguments(this IParameterSymbol parameter)
        => parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol.ToDisplayString().StartsWith(WellKnownTypes.PagingArguments);

    public static bool IsGlobalState(
        this IParameterSymbol parameter,
        [NotNullWhen(true)] out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (IsOrInheritsFrom(attributeData.AttributeClass, "HotChocolate.GlobalStateAttribute"))
            {
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg is { Key: "Key", Value.Value: string namedKeyValue })
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = parameter.Name;
                return true;
            }
        }

        return false;
    }

    public static bool IsScopedState(
        this IParameterSymbol parameter,
        [NotNullWhen(true)] out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (IsOrInheritsFrom(attributeData.AttributeClass, "HotChocolate.ScopedStateAttribute"))
            {
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg is { Key: "Key", Value.Value: string namedKeyValue })
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = parameter.Name;
                return true;
            }
        }

        return false;
    }

    public static bool IsLocalState(
        this IParameterSymbol parameter,
        [NotNullWhen(true)] out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (IsOrInheritsFrom(attributeData.AttributeClass, "HotChocolate.LocalStateAttribute"))
            {
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg is { Key: "Key", Value.Value: string namedKeyValue })
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = parameter.Name;
                return true;
            }
        }

        return false;
    }

    public static bool IsEventMessage(
        this IParameterSymbol parameter)
    {
        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == WellKnownAttributes.EventMessageAttribute)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsService(
        this IParameterSymbol parameter,
        out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == WellKnownAttributes.ServiceAttribute)
            {
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg is { Key: "Key", Value.Value: string namedKeyValue })
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = null;
                return true;
            }
        }

        return false;
    }

    public static bool IsArgument(
        this IParameterSymbol parameter,
        out string? key)
    {
        key = null;

        foreach (var attributeData in parameter.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == WellKnownAttributes.ArgumentAttribute)
            {
                if (attributeData.ConstructorArguments.Length == 1
                    && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                    && attributeData.ConstructorArguments[0].Value is string keyValue)
                {
                    key = keyValue;
                    return true;
                }

                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg is { Key: "Name", Value.Value: string namedKeyValue })
                    {
                        key = namedKeyValue;
                        return true;
                    }
                }

                key = null;
                return true;
            }
        }

        return false;
    }

    public static bool IsNonNullable(this IParameterSymbol parameter)
    {
        if (parameter.Type.NullableAnnotation != NullableAnnotation.NotAnnotated)
        {
            return false;
        }

        if (parameter.Type is INamedTypeSymbol namedTypeSymbol
            && namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return false;
        }

        return true;
    }

    public static ResolverResultKind GetResultKind(this IMethodSymbol method)
    {
        const string task = $"{WellKnownTypes.Task}<";
        const string valueTask = $"{WellKnownTypes.ValueTask}<";
        const string taskEnumerable = $"{WellKnownTypes.Task}<{WellKnownTypes.AsyncEnumerable}<";
        const string valueTaskEnumerable = $"{WellKnownTypes.ValueTask}<{WellKnownTypes.AsyncEnumerable}<";

        if (method.ReturnsVoid || method.ReturnsByRef || method.ReturnsByRefReadonly)
        {
            return ResolverResultKind.Invalid;
        }

        var returnType = method.ReturnType.ToDisplayString();

        if (returnType.Equals(WellKnownTypes.Task) || returnType.Equals(WellKnownTypes.ValueTask))
        {
            return ResolverResultKind.Invalid;
        }

        if (returnType.StartsWith(task) || returnType.StartsWith(valueTask))
        {
            if (returnType.StartsWith(taskEnumerable) || returnType.StartsWith(valueTaskEnumerable))
            {
                return ResolverResultKind.TaskAsyncEnumerable;
            }

            return ResolverResultKind.Task;
        }

        if (returnType.StartsWith(WellKnownTypes.Executable))
        {
            return ResolverResultKind.Executable;
        }

        if (returnType.StartsWith(WellKnownTypes.Queryable))
        {
            return ResolverResultKind.Queryable;
        }

        if (returnType.StartsWith(WellKnownTypes.AsyncEnumerable))
        {
            return ResolverResultKind.AsyncEnumerable;
        }

        return ResolverResultKind.Pure;
    }

    public static bool IsListType(this ISymbol member, [NotNullWhen(true)] out string? elementType)
    {
        if (member is IMethodSymbol methodSymbol)
        {
            return methodSymbol.ReturnType.IsListType(out elementType);
        }

        if (member is IPropertySymbol propertySymbol)
        {
            return propertySymbol.Type.IsListType(out elementType);
        }

        elementType = null;
        return false;
    }

    public static bool IsListType(this ITypeSymbol typeSymbol, [NotNullWhen(true)] out string? elementType)
    {
        typeSymbol = UnwrapWrapperTypes(typeSymbol);

        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            var typeDefinition = namedTypeSymbol.ConstructUnboundGenericType().ToDisplayString();

            if (WellKnownTypes.SupportedListInterfaces.Contains(typeDefinition))
            {
                elementType = namedTypeSymbol.TypeArguments[0].ToFullyQualified();
                return true;
            }

            if (typeDefinition.Equals(WellKnownTypes.EnumerableDefinition, StringComparison.Ordinal))
            {
                elementType = namedTypeSymbol.TypeArguments[0].ToFullyQualified();
                return true;
            }

            foreach (var interfaceType in namedTypeSymbol.AllInterfaces)
            {
                if (interfaceType.IsGenericType)
                {
                    var interfaceTypeDefinition = interfaceType.ConstructUnboundGenericType().ToDisplayString();
                    if (WellKnownTypes.SupportedListInterfaces.Contains(interfaceTypeDefinition))
                    {
                        elementType = interfaceType.TypeArguments[0].ToFullyQualified();
                        return true;
                    }
                }
            }
        }

        elementType = null;
        return false;
    }

    private static ITypeSymbol UnwrapWrapperTypes(ITypeSymbol typeSymbol)
    {
        while (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            var typeDefinition = namedTypeSymbol.ConstructUnboundGenericType().ToDisplayString();
            if (WellKnownTypes.TaskWrapper.Contains(typeDefinition))
            {
                typeSymbol = namedTypeSymbol.TypeArguments[0];
            }
            else
            {
                break;
            }
        }

        return typeSymbol;
    }

    public static bool HasPostProcessorAttribute(this ISymbol member)
    {
        foreach (var attributeData in member.GetAttributes())
        {
            if (IsPostProcessorAttribute(attributeData.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPostProcessorAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        while (attributeClass != null)
        {
            var typeName = attributeClass.ToDisplayString();
            if (typeName.Equals("HotChocolate.Types.UsePagingAttribute")
                || typeName.Equals("HotChocolate.Types.UseOffsetPagingAttribute"))
            {
                return true;
            }

            if (attributeClass.IsGenericType)
            {
                var typeDefinition = attributeClass.ConstructUnboundGenericType().ToDisplayString();
                if (typeDefinition == "HotChocolate.Types.UseResolverResultPostProcessorAttribute<>")
                {
                    return true;
                }
            }

            attributeClass = attributeClass.BaseType;
        }

        return false;
    }

    public static bool IsOrInheritsFrom(this ITypeSymbol? attributeClass, params string[] fullTypeName)
    {
        var current = attributeClass;

        while (current != null)
        {
            foreach (var typeName in fullTypeName)
            {
                if (current.ToDisplayString() == typeName)
                {
                    return true;
                }
            }

            current = current.BaseType;
        }

        return false;
    }

    public static bool IsOrInheritsFrom(this ITypeSymbol? attributeClass, string fullTypeName)
    {
        var current = attributeClass;

        while (current != null)
        {
            if (current.ToDisplayString() == fullTypeName)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    public static ITypeSymbol? GetReturnType(this ISymbol member)
    {
        ITypeSymbol? returnType;

        switch (member)
        {
            case IMethodSymbol method:
                returnType = method.ReturnType;
                break;

            case IPropertySymbol property:
                returnType = property.Type;
                break;

            case IParameterSymbol parameter:
                returnType = parameter.Type;
                break;

            default:
                return null;
        }

        if (returnType is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } namedType)
        {
            var originalDefinition = namedType.ConstructedFrom;

            if (originalDefinition.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks"
                && originalDefinition.Name is "ValueTask" or "Task")
            {
                return namedType.TypeArguments[0];
            }
        }

        return returnType;
    }

    public static bool IsConnectionType(this INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        var connectionInterface = compilation.GetTypeByMetadataName("HotChocolate.Types.Pagination.IConnection`1");

        if (connectionInterface == null)
        {
            return false;
        }

        return typeSymbol.AllInterfaces.Any(
            s => SymbolEqualityComparer.Default.Equals(s.OriginalDefinition, connectionInterface));
    }

    /// <summary>
    /// Determines if the method is an accessor (e.g., get_Property, set_Property).
    /// </summary>
    public static bool IsPropertyOrEventAccessor(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.PropertyGet
            || method.MethodKind == MethodKind.PropertySet
            || method.MethodKind == MethodKind.EventAdd
            || method.MethodKind == MethodKind.EventRemove
            || method.MethodKind == MethodKind.EventRaise;
    }

    /// <summary>
    /// Determines if the method is an operator overload (e.g., op_Addition).
    /// </summary>
    public static bool IsOperator(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.UserDefinedOperator
            || method.MethodKind == MethodKind.Conversion;
    }

    public static bool IsConstructor(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.Constructor
            || method.MethodKind == MethodKind.SharedConstructor;
    }

    public static bool IsSpecialMethod(this IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.Destructor
            || method.MethodKind == MethodKind.LocalFunction
            || method.MethodKind == MethodKind.AnonymousFunction
            || method.MethodKind == MethodKind.DelegateInvoke;
    }

    public static bool IsCompilerGenerated(this IMethodSymbol method)
        => method
            .GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString()
                == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    public static IEnumerable<ISymbol> AllPublicInstanceMembers(this ITypeSymbol type)
    {
        var processed = PooledObjects.GetStringSet();
        var current = type;

        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in current.GetMembers())
            {
                if (member.DeclaredAccessibility == Accessibility.Public
                    && member.Kind is SymbolKind.Property or SymbolKind.Method
                    && !member.IsStatic
                    && !member.IsIgnored()
                    && processed.Add(member.Name))
                {
                    yield return member;
                }
            }

            current = current.BaseType;
        }

        processed.Clear();
        PooledObjects.Return(processed);
    }

    public static DirectiveScope GetShareableScope(this ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass.IsOrInheritsFrom(WellKnownAttributes.ShareableAttribute))
            {
                var isScoped = attribute.ConstructorArguments.Length > 0
                    && attribute.ConstructorArguments[0].Value is true;
                return isScoped ? DirectiveScope.Field : DirectiveScope.Type;
            }
        }

        return DirectiveScope.None;
    }

    public static bool IsNodeResolver(this ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass.IsOrInheritsFrom(WellKnownAttributes.NodeResolverAttribute))
            {
                return true;
            }
        }

        return false;
    }

    public static DirectiveScope GetInaccessibleScope(this ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass.IsOrInheritsFrom(WellKnownAttributes.InaccessibleAttribute))
            {
                var isScoped = attribute.ConstructorArguments.Length > 0
                    && attribute.ConstructorArguments[0].Value is true;
                return isScoped ? DirectiveScope.Field : DirectiveScope.Type;
            }
        }

        return DirectiveScope.None;
    }

    public static ImmutableArray<AttributeData> GetUserAttributes(this ImmutableArray<AttributeData> attributes)
    {
        var mutated = attributes;

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass.IsOrInheritsFrom(WellKnownAttributes.ShareableAttribute)
                || attribute.AttributeClass.IsOrInheritsFrom(WellKnownAttributes.InaccessibleAttribute)
                || !attribute.AttributeClass.IsOrInheritsFrom(WellKnownAttributes.DescriptorAttribute))
            {
                mutated = mutated.Remove(attribute);
            }
        }

        return mutated;
    }
}
