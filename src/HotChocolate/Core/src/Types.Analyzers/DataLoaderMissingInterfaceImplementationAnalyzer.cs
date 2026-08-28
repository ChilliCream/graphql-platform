using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataLoaderMissingInterfaceImplementationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderMissingInterfaceImplementation];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;
        var attributes = GenericDataLoaderAnalyzerHelper.GetGenericDataLoaderAttributes(
            methodSymbol,
            context.Compilation);

        if (attributes.Length != 1
            || !GenericDataLoaderAnalyzerHelper.HasExactlyOneDataLoaderAttribute(
                methodSymbol,
                context.Compilation)
            || !GenericDataLoaderAnalyzerHelper.IsValidGenericDataLoaderMethod(
                methodSymbol,
                attributes[0],
                context.Compilation)
            || attributes[0].AttributeClass?.TypeArguments[0] is not INamedTypeSymbol dataLoaderType
            || !HasUnimplementedExtraMembers(
                dataLoaderType,
                methodSymbol,
                attributes[0],
                context.Compilation,
                context.CancellationToken,
                out var generatedTypeName))
        {
            return;
        }

        var location = attributes[0].ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)
            .GetLocation()
            ?? methodSymbol.Locations.FirstOrDefault()
            ?? Location.None;

        context.ReportDiagnostic(Diagnostic.Create(
            Errors.DataLoaderMissingInterfaceImplementation,
            location,
            dataLoaderType.ToDisplayString(),
            generatedTypeName));
    }

    private static bool HasUnimplementedExtraMembers(
        INamedTypeSymbol dataLoaderType,
        IMethodSymbol methodSymbol,
        AttributeData attribute,
        Compilation compilation,
        CancellationToken cancellationToken,
        out string generatedTypeName)
    {
        generatedTypeName = DataLoaderInfo.GetDataLoaderName(methodSymbol.Name, attribute) + "DataLoader";
        var extraMembers = GetExtraMembers(dataLoaderType).ToArray();

        if (extraMembers.Length == 0)
        {
            return false;
        }

        var generatedType = compilation.GetSymbolsWithName(
                generatedTypeName,
                SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault(t => t.TypeKind is TypeKind.Class
                && t.Arity == 0
                && SymbolEqualityComparer.Default.Equals(
                    t.ContainingNamespace,
                    methodSymbol.ContainingNamespace)
                && IsPartialClass(t, cancellationToken));

        if (generatedType is null)
        {
            return true;
        }

        foreach (var member in extraMembers)
        {
            if (!IsImplementedBy(generatedType, member))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPartialClass(INamedTypeSymbol type, CancellationToken cancellationToken)
        => type.DeclaringSyntaxReferences.Any(t =>
            t.GetSyntax(cancellationToken) is ClassDeclarationSyntax classDeclaration
            && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static IEnumerable<ISymbol> GetExtraMembers(
        INamedTypeSymbol dataLoaderType)
    {
        foreach (var member in dataLoaderType.GetMembers())
        {
            if (IsAbstractInterfaceMember(member))
            {
                yield return member;
            }
        }
    }

    private static bool IsAbstractInterfaceMember(ISymbol member)
        => member switch
        {
            IMethodSymbol method => method.IsAbstract && method.AssociatedSymbol is null,
            IPropertySymbol property => property.IsAbstract,
            IEventSymbol @event => @event.IsAbstract,
            _ => false
        };

    private static bool IsImplementedBy(INamedTypeSymbol generatedType, ISymbol interfaceMember)
        => interfaceMember switch
        {
            IMethodSymbol method => generatedType.GetMembers(method.Name)
                .OfType<IMethodSymbol>()
                .Any(candidate => IsMethodImplementation(candidate, method)),
            IPropertySymbol property => generatedType.GetMembers(property.Name)
                .OfType<IPropertySymbol>()
                .Any(candidate => IsPropertyImplementation(candidate, property)),
            IEventSymbol @event => generatedType.GetMembers(@event.Name)
                .OfType<IEventSymbol>()
                .Any(candidate => !candidate.IsStatic
                    && candidate.DeclaredAccessibility is Accessibility.Public
                    && SymbolEqualityComparer.Default.Equals(candidate.Type, @event.Type)),
            _ => false
        };

    private static bool IsMethodImplementation(IMethodSymbol candidate, IMethodSymbol interfaceMethod)
    {
        if (candidate.IsStatic
            || candidate.DeclaredAccessibility is not Accessibility.Public
            || candidate.MethodKind is not MethodKind.Ordinary
            || candidate.Arity != interfaceMethod.Arity
            || !SymbolEqualityComparer.Default.Equals(candidate.ReturnType, interfaceMethod.ReturnType)
            || candidate.Parameters.Length != interfaceMethod.Parameters.Length)
        {
            return false;
        }

        for (var i = 0; i < candidate.Parameters.Length; i++)
        {
            if (candidate.Parameters[i].RefKind != interfaceMethod.Parameters[i].RefKind
                || !SymbolEqualityComparer.Default.Equals(
                    candidate.Parameters[i].Type,
                    interfaceMethod.Parameters[i].Type))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPropertyImplementation(
        IPropertySymbol candidate,
        IPropertySymbol interfaceProperty)
    {
        if (candidate.IsStatic
            || candidate.DeclaredAccessibility is not Accessibility.Public
            || !SymbolEqualityComparer.Default.Equals(candidate.Type, interfaceProperty.Type)
            || candidate.Parameters.Length != interfaceProperty.Parameters.Length
            || !HasRequiredAccessor(candidate.GetMethod, interfaceProperty.GetMethod)
            || !HasRequiredAccessor(candidate.SetMethod, interfaceProperty.SetMethod))
        {
            return false;
        }

        for (var i = 0; i < candidate.Parameters.Length; i++)
        {
            if (candidate.Parameters[i].RefKind != interfaceProperty.Parameters[i].RefKind
                || !SymbolEqualityComparer.Default.Equals(
                    candidate.Parameters[i].Type,
                    interfaceProperty.Parameters[i].Type))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasRequiredAccessor(IMethodSymbol? candidate, IMethodSymbol? interfaceAccessor)
        => interfaceAccessor is null
            || candidate is { DeclaredAccessibility: Accessibility.Public };
}
