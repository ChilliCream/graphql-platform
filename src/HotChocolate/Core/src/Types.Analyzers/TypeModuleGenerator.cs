using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;
using static HotChocolate.Types.Analyzers.WellKnownTypes;
using static HotChocolate.Types.Analyzers.WellKnownFileNames;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class TypeModuleGenerator : IIncrementalGenerator
{
    private static readonly AttributeTargetSpecifierSyntax assemblyTarget =
        AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword));

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ModuleOrType> modulesAndTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsRelevant(s),
                    transform: TryGetModuleOrType)
                .Where(static t => !t.IsEmpty);

        var valueProvider = context.CompilationProvider.Combine(modulesAndTypes.Collect());

        context.RegisterSourceOutput(
            valueProvider,
            static (context, source) => Execute(context, source.Item1, source.Item2));
    }

    private static bool IsRelevant(SyntaxNode node)
        => IsClassWithAttribute(node) || IsAssemblyAttributeList(node);

    private static bool IsClassWithAttribute(SyntaxNode node)
        => node is ClassDeclarationSyntax m &&
            (m.AttributeLists.Count > 0 || m.BaseList is { Types.Count: > 0 });

    private static bool IsAssemblyAttributeList(SyntaxNode node)
        => node is AttributeListSyntax;

    private static ModuleOrType TryGetModuleOrType(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.Node is ClassDeclarationSyntax possibleType)
        {
            if (possibleType.AttributeLists.Count > 0)
            {
                foreach (AttributeListSyntax attributeListSyntax in possibleType.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        ISymbol? symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                        IMethodSymbol? attributeSymbol = symbol as IMethodSymbol;
                        if (attributeSymbol is null)
                        {
                            continue;
                        }

                        INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        string fullName = attributeContainingTypeSymbol.ToDisplayString();

                        if (TypeAttributes.Contains(fullName))
                        {
                            var model = context.SemanticModel.GetDeclaredSymbol(possibleType);

                            if (model is ITypeSymbol type)
                            {
                                return ModuleOrType.Type(type.ToDisplayString());
                            }
                        }
                    }
                }
            }

            if (possibleType.BaseList is not null && possibleType.BaseList.Types.Count > 0)
            {
                var model = context.SemanticModel.GetDeclaredSymbol(possibleType);
                if (model is ITypeSymbol type)
                {
                    ITypeSymbol? current = type.BaseType;
                    while (current is not null)
                    {
                        string displayString = current.ToDisplayString();

                        if (displayString.Equals(SystemObject, StringComparison.Ordinal))
                        {
                            break;
                        }

                        if (BaseTypes.Contains(displayString))
                        {
                            return ModuleOrType.Type(type.ToDisplayString());
                        }

                        current = current.BaseType;
                    }
                }
            }
        }
        else if (context.Node is AttributeListSyntax attributeList)
        {
            foreach (AttributeSyntax attributeSyntax in attributeList.Attributes)
            {
                ISymbol? symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                IMethodSymbol? attributeSymbol = symbol as IMethodSymbol;
                if (attributeSymbol is null)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName.Equals(ModuleName, StringComparison.Ordinal))
                {
                    if (attributeSyntax.ArgumentList?.Arguments.SingleOrDefault() is { } argument)
                    {
                        return ModuleOrType.Module(argument.ToString().Trim('\"'));
                    }
                }
            }
        }

        return default;
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ModuleOrType> modulesAndTypes)
    {
        const string indent = "    ";

        ModuleOrType module = modulesAndTypes.FirstOrDefault(t => t.ModuleName is not null);

        if (module.IsEmpty)
        {
            module = ModuleOrType.Module(
                compilation.AssemblyName?.Split('.').Last() + "Types" ??
                "AssemblyTypes");
        }

        var code = new StringBuilder();

        code.AppendLine("using System;");
        code.AppendLine("using HotChocolate.Execution.Configuration;");
        code.AppendLine();

        if (!modulesAndTypes.IsDefaultOrEmpty && modulesAndTypes.Any(t => t.TypeName is not null))
        {
            code.AppendLine();
            code.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            code.AppendLine("{");
            code.AppendLine($"{indent}public static class {module.ModuleName}RequestExecutorBuilderExtensions");
            code.AppendLine($"{indent}{{");
            code.AppendLine($"{indent}{indent}public static IRequestExecutorBuilder Add{module.ModuleName}(this IRequestExecutorBuilder builder)");
            code.AppendLine($"{indent}{indent}{{");

            foreach (var type in modulesAndTypes.Where(t => t.TypeName is not null).Distinct())
            {
                code.AppendLine($"{indent}{indent}{indent}builder.AddTypeExtension<{type.TypeName}>();");
            }
            code.AppendLine($"{indent}{indent}{indent}return builder;");
            code.AppendLine($"{indent}{indent}}}");
            code.AppendLine($"{indent}}}");
            code.AppendLine("}");
        }

        context.AddSource(TypeModuleFile, SourceText.From(code.ToString(), Encoding.UTF8));
    }

    private readonly struct ModuleOrType
    {
        private readonly bool _isInit;

        private ModuleOrType(string? typeName, string? moduleName)
        {
            TypeName = typeName;
            ModuleName = moduleName;
            _isInit = true;
        }

        public bool IsEmpty => !_isInit;

        public string? TypeName { get; }

        public string? ModuleName { get; }

        public static ModuleOrType Type(string typeName)
            => new(typeName, null);

        public static ModuleOrType Module(string moduleName)
            => new(null, moduleName);
    }
}
