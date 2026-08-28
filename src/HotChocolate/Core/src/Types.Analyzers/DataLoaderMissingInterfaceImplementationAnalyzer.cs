using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
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
        var extraMembers = GetRequiredExtraMembers(dataLoaderType, compilation).ToArray();

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
                && t.ContainingType is null
                && SymbolEqualityComparer.Default.Equals(
                    t.ContainingNamespace,
                    methodSymbol.ContainingNamespace)
                && IsPartialClass(t, cancellationToken));

        if (generatedType is null)
        {
            return true;
        }

        if (!GenericDataLoaderAnalyzerHelper.TryResolveContract(
                dataLoaderType,
                compilation,
                out var contract))
        {
            return false;
        }

        var generatedTypeWithInterface = GetGeneratedTypeWithInterface(
            generatedType,
            dataLoaderType,
            contract,
            compilation,
            cancellationToken,
            out var compilationWithInterface);

        if (generatedTypeWithInterface is null)
        {
            return true;
        }

        var implementedDataLoaderType = generatedTypeWithInterface.Interfaces.FirstOrDefault(t =>
            t.ToFullyQualifiedWithNullRefQualifier()
                .Equals(dataLoaderType.ToFullyQualifiedWithNullRefQualifier(), StringComparison.Ordinal));

        if (implementedDataLoaderType is null)
        {
            return true;
        }

        foreach (var member in GetRequiredExtraMembers(implementedDataLoaderType, compilationWithInterface))
        {
            if (generatedTypeWithInterface.FindImplementationForInterfaceMember(member) is null)
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

    private static INamedTypeSymbol? GetGeneratedTypeWithInterface(
        INamedTypeSymbol generatedType,
        INamedTypeSymbol dataLoaderType,
        DataLoaderContract contract,
        Compilation compilation,
        CancellationToken cancellationToken,
        out Compilation compilationWithInterface)
    {
        var @namespace = generatedType.ContainingNamespace.ToDisplayString();
        var namespaceDeclaration = @namespace.Length == 0
            ? string.Empty
            : $"namespace {@namespace};";
        var source = $$"""
            #nullable enable
            {{namespaceDeclaration}}

            partial class {{generatedType.Name}}
                : global::GreenDonut.DataLoaderBase<{{contract.KeyType.ToFullyQualifiedWithNullRefQualifier()}}, {{contract.ValueType.ToFullyQualifiedWithNullRefQualifier()}}>,
                  {{dataLoaderType.ToFullyQualifiedWithNullRefQualifier()}}
            {
            }
            """;
        var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken);
        compilationWithInterface = compilation.AddSyntaxTrees(syntaxTree);

        return compilationWithInterface.GetSymbolsWithName(generatedType.Name, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault(t => t.TypeKind is TypeKind.Class
                && t.Arity == 0
                && t.ContainingType is null
                && t.ContainingNamespace.ToDisplayString().Equals(@namespace, StringComparison.Ordinal));
    }

    private static IEnumerable<ISymbol> GetRequiredExtraMembers(
        INamedTypeSymbol dataLoaderType,
        Compilation compilation)
    {
        var dataLoaderInterfaces = new HashSet<ISymbol>(
            [
                compilation.GetTypeByMetadataName("GreenDonut.IDataLoader")!,
                compilation.GetTypeByMetadataName("GreenDonut.IDataLoader`2")!,
                compilation.GetTypeByMetadataName("GreenDonut.IBatchDataLoader`2")!,
                compilation.GetTypeByMetadataName("GreenDonut.ICacheDataLoader`2")!
            ],
            SymbolEqualityComparer.Default);

        foreach (var interfaceType in dataLoaderType.AllInterfaces.Prepend(dataLoaderType))
        {
            if (dataLoaderInterfaces.Contains(interfaceType.OriginalDefinition))
            {
                continue;
            }

            foreach (var member in interfaceType.GetMembers())
            {
                if (IsAbstractInterfaceMember(member))
                {
                    yield return member;
                }
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
}
