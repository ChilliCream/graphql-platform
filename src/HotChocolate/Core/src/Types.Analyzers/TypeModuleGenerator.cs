using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.Types.Analyzers.WellKnownFileNames;
using TypeInfo = HotChocolate.Types.Analyzers.Inspectors.TypeInfo;

namespace HotChocolate.Types.Analyzers;

[Generator]
public class TypeModuleGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] _inspectors =
    [
        new TypeAttributeInspector(),
        new ClassBaseClassInspector(),
        new ModuleInspector(),
        new DataLoaderInspector(),
        new DataLoaderDefaultsInspector(),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(c => PostInitialization(c));

        var modulesAndTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsRelevant(s),
                    transform: TryGetModuleOrType)
                .Where(static t => t is not null)!
                .WithComparer(SyntaxInfoComparer.Default);

        var valueProvider = context.CompilationProvider.Combine(modulesAndTypes.Collect());

        context.RegisterSourceOutput(
            valueProvider,
            static (context, source) => Execute(context, source.Left, source.Right));
    }

    private static void PostInitialization(IncrementalGeneratorPostInitializationContext context)
    {
        
    }

    private static bool IsRelevant(SyntaxNode node)
        => IsTypeWithAttribute(node) ||
            IsClassWithBaseClass(node) ||
            IsAssemblyAttributeList(node) ||
            IsMethodWithAttribute(node);

    private static bool IsClassWithBaseClass(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0, };

    private static bool IsTypeWithAttribute(SyntaxNode node)
        => node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0, };

    private static bool IsMethodWithAttribute(SyntaxNode node)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0, };

    private static bool IsAssemblyAttributeList(SyntaxNode node)
        => node is AttributeListSyntax;

    private static ISyntaxInfo? TryGetModuleOrType(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _inspectors.Length; i++)
        {
            if (_inspectors[i].TryHandle(context, out var syntaxInfo))
            {
                return syntaxInfo;
            }
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ISyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }
       
        var module = syntaxInfos.GetModuleInfo(compilation.AssemblyName, out var defaultModule);

        // if there is only the module info we do not need to generate a module.
        if (!defaultModule && syntaxInfos.Length == 1)
        {
            return;
        }

        WriteConfiguration(context, syntaxInfos, module);
    }
    
    private static void WriteConfiguration(
        SourceProductionContext context,
        ImmutableArray<ISyntaxInfo> syntaxInfos,
        ModuleInfo module)
    {
        using var generator = new ModuleSyntaxGenerator(module.ModuleName, "Microsoft.Extensions.DependencyInjection");
        
        generator.WriterHeader();
        generator.WriteBeginNamespace();
        generator.WriteBeginClass();
        generator.WriteBeginRegistrationMethod();
        
        var operations = OperationType.No;

        foreach (var syntaxInfo in syntaxInfos)
        {
            switch (syntaxInfo)
            {
                case TypeInfo type:
                    if ((module.Options & ModuleOptions.RegisterTypes) ==
                        ModuleOptions.RegisterTypes)
                    {
                        generator.WriteRegisterType(type.Name);
                    }
                    break;

                case TypeExtensionInfo extension:
                    if ((module.Options & ModuleOptions.RegisterTypes) ==
                        ModuleOptions.RegisterTypes)
                    {
                        generator.WriteRegisterTypeExtension(extension.Name, extension.IsStatic);

                        if (extension.Type is not OperationType.No &&
                            (operations & extension.Type) != extension.Type)
                        {
                            operations |= extension.Type;
                        }
                    }
                    break;

                case RegisterDataLoaderInfo dataLoader:
                    if ((module.Options & ModuleOptions.RegisterDataLoader) ==
                        ModuleOptions.RegisterDataLoader)
                    {
                        generator.WriteRegisterDataLoader(dataLoader.Name);
                    }
                    break;
            }
        }
        
        if ((operations & OperationType.Query) == OperationType.Query)
        {
            generator.WriteTryAddOperationType(OperationType.Query);
        }

        if ((operations & OperationType.Mutation) == OperationType.Mutation)
        {
            generator.WriteTryAddOperationType(OperationType.Mutation);
        }

        if ((operations & OperationType.Subscription) == OperationType.Subscription)
        {
            generator.WriteTryAddOperationType(OperationType.Subscription);
        }
        
        generator.WriteEndRegistrationMethod();
        generator.WriteEndClass();
        generator.WriteEndNamespace();
        
        context.AddSource(TypeModuleFile, generator.ToSourceText());
    }
}
