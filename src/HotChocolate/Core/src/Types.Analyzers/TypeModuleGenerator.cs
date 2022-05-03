using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;
using static HotChocolate.Types.Analyzers.WellKnownTypes;
using static HotChocolate.Types.Analyzers.WellKnownFileNames;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class TypeModuleGenerator : IIncrementalGenerator
{
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
                        var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                        if (symbol is not IMethodSymbol attributeSymbol)
                        {
                            continue;
                        }

                        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        var fullName = attributeContainingTypeSymbol.ToDisplayString();

                        if (ExtendObjectTypeAttribute.Equals(fullName, StringComparison.Ordinal))
                        {
                            var model = context.SemanticModel.GetDeclaredSymbol(possibleType);

                            if (model is ITypeSymbol type)
                            {
                                return ModuleOrType.Type(
                                    type.ToDisplayString(),
                                    TypeKind.TypeExtension);
                            }

                        }
                        else if (TypeAttributes.Contains(fullName))
                        {
                            var model = context.SemanticModel.GetDeclaredSymbol(possibleType);

                            if (model is ITypeSymbol type)
                            {
                                return ModuleOrType.Type(
                                    type.ToDisplayString(),
                                    TypeKind.Type);
                            }
                        }
                    }
                }
            }

            if (possibleType.BaseList is not null && possibleType.BaseList.Types.Count > 0)
            {
                var model = context.SemanticModel.GetDeclaredSymbol(possibleType);
                if (model is { IsAbstract: false } type)
                {
                    var typeDisplayString = type.ToDisplayString();
                    var processing = new Queue<INamedTypeSymbol>();
                    processing.Enqueue(type);

                    var current = type.BaseType;

                    while (current is not null)
                    {
                        processing.Enqueue(current);

                        var displayString = current.ToDisplayString();

                        if (displayString.Equals(SystemObject, StringComparison.Ordinal))
                        {
                            break;
                        }

                        if (TypeClass.Contains(displayString))
                        {
                            return ModuleOrType.Type(typeDisplayString, TypeKind.Type);
                        }
                        else if (TypeExtensionClass.Contains(displayString))
                        {
                            return ModuleOrType.Type(typeDisplayString, TypeKind.TypeExtension);
                        }

                        current = current.BaseType;
                    }

                    while (processing.Count > 0)
                    {
                        current = processing.Dequeue();

                        var displayString = current.ToDisplayString();

                        if (displayString.Equals(DataLoader, StringComparison.Ordinal))
                        {
                            return ModuleOrType.Type(typeDisplayString, TypeKind.DataLoader);
                        }

                        foreach (var interfaceType in current.Interfaces)
                        {
                            processing.Enqueue(interfaceType);
                        }
                    }

                }
            }
        }
        else if (context.Node is AttributeListSyntax attributeList)
        {
            foreach (AttributeSyntax attributeSyntax in attributeList.Attributes)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                if (symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName.Equals(ModuleAttribute, StringComparison.Ordinal) &&
                    attributeSyntax.ArgumentList is { Arguments.Count: > 0 })
                {
                    var nameExpr = attributeSyntax.ArgumentList.Arguments[0].Expression;
                    var name = context.SemanticModel.GetConstantValue(nameExpr).ToString();

                    var features = (int)ModuleOptions.Default;
                    if (attributeSyntax.ArgumentList.Arguments.Count > 1)
                    {
                        var featuresExpr = attributeSyntax.ArgumentList.Arguments[1].Expression;
                        features = (int)context.SemanticModel.GetConstantValue(featuresExpr).Value!;
                    }

                    return ModuleOrType.Module(name, (ModuleOptions)features);
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
                "AssemblyTypes",
                ModuleOptions.Default);
        }

        var code = new StringBuilder();

        code.AppendLine("using System;");
        code.AppendLine("using HotChocolate.Execution.Configuration;");

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
                switch (type.TypeKind)
                {
                    case TypeKind.Type:
                        if ((module.Options & ModuleOptions.RegisterTypes) == ModuleOptions.RegisterTypes)
                        {
                            code.AppendLine($"{indent}{indent}{indent}builder.AddType<{type.TypeName}>();");
                        }
                        break;

                    case TypeKind.TypeExtension:
                        if ((module.Options & ModuleOptions.RegisterTypes) == ModuleOptions.RegisterTypes)
                        {
                            code.AppendLine($"{indent}{indent}{indent}builder.AddTypeExtension<{type.TypeName}>();");
                        }
                        break;

                    case TypeKind.DataLoader:
                        if ((module.Options & ModuleOptions.RegisterDataLoader) == ModuleOptions.RegisterDataLoader)
                        {
                            code.AppendLine($"{indent}{indent}{indent}builder.AddDataLoader<{type.TypeName}>();");
                        }
                        break;
                }
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

        private ModuleOrType(
            string? typeName,
            TypeKind typeKind,
            string? moduleName,
            ModuleOptions options)
        {
            TypeName = typeName;
            TypeKind = typeKind;
            ModuleName = moduleName;
            Options = options;
            _isInit = true;
        }

        public bool IsEmpty => !_isInit;

        public string? TypeName { get; }

        public TypeKind TypeKind { get; }

        public string? ModuleName { get; }

        public ModuleOptions Options { get; }

        public override bool Equals(object obj)
            => obj is ModuleOrType m && Equals(m);

        public bool Equals(ModuleOrType other)
            => IsEmpty == other.IsEmpty &&
                TypeKind == other.TypeKind &&
                Options == other.Options &&
                string.Equals(TypeName, other.TypeName, StringComparison.Ordinal) &&
                string.Equals(ModuleName, other.ModuleName, StringComparison.Ordinal);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _isInit.GetHashCode();
                hashCode = (hashCode * 397) ^ (TypeName != null ? TypeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) TypeKind;
                hashCode = (hashCode * 397) ^ (ModuleName != null ? ModuleName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) Options;
                return hashCode;
            }
        }

        public static ModuleOrType Type(string name, TypeKind kind)
            => new(name, kind, null, ModuleOptions.Default);

        public static ModuleOrType Module(string name, ModuleOptions options)
            => new(null, TypeKind.Unknown, name, options);
    }

    private enum TypeKind
    {
        Unknown = 0,
        Type,
        TypeExtension,
        DataLoader
    }

    [Flags]
    public enum ModuleOptions
    {
        Default = RegisterDataLoader | RegisterTypes,
        RegisterTypes = 1,
        RegisterDataLoader = 2
    }
}
