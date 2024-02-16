using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;
using static HotChocolate.Types.Analyzers.WellKnownFileNames;
using static HotChocolate.Types.Analyzers.WellKnownTypes;
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
        new OperationInspector(),
        new RequestMiddlewareInspector(),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

    private static bool IsRelevant(SyntaxNode node)
        => IsTypeWithAttribute(node) ||
            IsClassWithBaseClass(node) ||
            IsAssemblyAttributeList(node) ||
            IsMethodWithAttribute(node) ||
            IsMiddlewareMethod(node);

    private static bool IsClassWithBaseClass(SyntaxNode node)
        => node is ClassDeclarationSyntax { BaseList.Types.Count: > 0, };

    private static bool IsTypeWithAttribute(SyntaxNode node)
        => node is BaseTypeDeclarationSyntax { AttributeLists.Count: > 0, };

    private static bool IsMethodWithAttribute(SyntaxNode node)
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0, };

    private static bool IsAssemblyAttributeList(SyntaxNode node)
        => node is AttributeListSyntax;

    private static bool IsMiddlewareMethod(SyntaxNode node)
        => node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: var method,
                },
            } &&
            (method.Equals("UseRequest") || method.Equals("UseField") || method.Equals("Use")); 

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
        var dataLoaderDefaults = syntaxInfos.GetDataLoaderDefaults();

        // if there is only the module info we do not need to generate a module.
        if (!defaultModule && syntaxInfos.Length == 1)
        {
            return;
        }

        var syntaxInfoList = syntaxInfos.ToList();
        WriteOperationTypes(context, syntaxInfoList, module);
        WriteDataLoader(context, syntaxInfoList, dataLoaderDefaults);
        WriteConfiguration(context, syntaxInfoList, module);
    }

    private static void WriteConfiguration(
        SourceProductionContext context,
        List<ISyntaxInfo> syntaxInfos,
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
                
                case DataLoaderInfo dataLoader:
                    if ((module.Options & ModuleOptions.RegisterDataLoader) ==
                        ModuleOptions.RegisterDataLoader)
                    {
                        var typeName = $"{dataLoader.Namespace}.{dataLoader.Name}";
                        var interfaceTypeName = $"{dataLoader.Namespace}.{dataLoader.InterfaceName}";
                        
                        generator.WriteRegisterDataLoader(typeName, interfaceTypeName);
                    }
                    break;
                
                case OperationRegistrationInfo operation:
                    if ((module.Options & ModuleOptions.RegisterTypes) ==
                        ModuleOptions.RegisterTypes)
                    {
                        generator.WriteRegisterTypeExtension(operation.TypeName, false);
                        
                        if (operation.Type is not OperationType.No &&
                            (operations & operation.Type) != operation.Type)
                        {
                            operations |= operation.Type;
                        }
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

        var middleware = 0;
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is RequestMiddlewareInfo requestMiddleware)
            {
                middleware++;
                generator.WriteMiddlewareExtensionMethod($"Middleware_{middleware}", requestMiddleware.Location);
            }
        }
        
        generator.WriteEndClass();
        generator.WriteEndNamespace();

        context.AddSource(TypeModuleFile, generator.ToSourceText());
    }

    private static void WriteDataLoader(
        SourceProductionContext context,
        List<ISyntaxInfo> syntaxInfos,
        DataLoaderDefaultsInfo defaults)
    {
        var dataLoaders = new List<DataLoaderInfo>();

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is not DataLoaderInfo dataLoader)
            {
                continue;
            }

            if (dataLoader.MethodSymbol.Parameters.Length == 0)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Errors.KeyParameterMissing,
                        Location.Create(
                            dataLoader.MethodSyntax.SyntaxTree,
                            dataLoader.MethodSyntax.ParameterList.Span)));
                continue;
            }

            if (dataLoader.MethodSymbol.DeclaredAccessibility is not Accessibility.Public
                and not Accessibility.Internal and not Accessibility.ProtectedAndInternal)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Errors.MethodAccessModifierInvalid,
                        Location.Create(
                            dataLoader.MethodSyntax.SyntaxTree,
                            dataLoader.MethodSyntax.Modifiers.Span)));
                continue;
            }

            dataLoaders.Add(dataLoader);
        }
        
        using var generator = new DataLoaderSyntaxGenerator();
        generator.WriterHeader();

        foreach (var group in dataLoaders.GroupBy(t => t.Namespace))
        {
            generator.WriteBeginNamespace(group.Key);
            
            foreach (var dataLoader in group)
            {
                var keyArg = dataLoader.MethodSymbol.Parameters[0];
                var keyType = keyArg.Type;
                var cancellationTokenIndex = -1;
                var serviceMap = new Dictionary<int, string>();

                if (IsKeysArgument(keyType))
                {
                    keyType = ExtractKeyType(keyType);
                }

                InspectDataLoaderParameters(
                    dataLoader,
                    ref cancellationTokenIndex,
                    serviceMap);

                DataLoaderKind kind;

                if (IsReturnTypeDictionary(dataLoader.MethodSymbol.ReturnType, keyType))
                {
                    kind = DataLoaderKind.Batch;
                }
                else if (IsReturnTypeLookup(dataLoader.MethodSymbol.ReturnType, keyType))
                {
                    kind = DataLoaderKind.Group;
                }
                else
                {
                    keyType = keyArg.Type;
                    kind = DataLoaderKind.Cache;
                }

                var valueType = ExtractValueType(dataLoader.MethodSymbol.ReturnType, kind);

                GenerateDataLoader(
                    generator,
                    dataLoader,
                    defaults,
                    kind,
                    keyType,
                    valueType,
                    dataLoader.MethodSymbol.Parameters.Length,
                    cancellationTokenIndex,
                    serviceMap);
            }
            
            generator.WriteEndNamespace();
        }
        
        context.AddSource(DataLoaderFile, generator.ToSourceText());
    }

    private static void WriteOperationTypes(
        SourceProductionContext context,
        List<ISyntaxInfo> syntaxInfos,
        ModuleInfo module)
    {
        var operations = new List<OperationInfo>();

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is OperationInfo operation)
            {
                operations.Add(operation);    
            }
        }

        if (operations.Count == 0)
        {
            return;
        }

        using var generator = new OperationFieldSyntaxGenerator();
        generator.WriterHeader();
        generator.WriteBeginNamespace("Microsoft.Extensions.DependencyInjection");

        foreach (var group in operations.GroupBy(t => t.Type))
        {
            var typeName = $"{module.ModuleName}{group.Key}Type";
            
            generator.WriteBeginClass(typeName);
            generator.WriteConfigureMethod(group.Key, group);
            generator.WriteEndClass();

            syntaxInfos.Add(new OperationRegistrationInfo(
                group.Key,
                $"Microsoft.Extensions.DependencyInjection.{typeName}"));
        }
        
        generator.WriteEndNamespace();
        
        context.AddSource(RootTypesFile, generator.ToSourceText());
    }

    private static void GenerateDataLoader(
        DataLoaderSyntaxGenerator generator,
        DataLoaderInfo dataLoader,
        DataLoaderDefaultsInfo defaults,
        DataLoaderKind kind,
        ITypeSymbol keyType,
        ITypeSymbol valueType,
        int parameterCount,
        int cancelIndex,
        Dictionary<int, string> services)
    {
        var isScoped = dataLoader.IsScoped ?? defaults.Scoped ?? false;
        var isPublic = dataLoader.IsPublic ?? defaults.IsPublic ?? true;
        var isInterfacePublic = dataLoader.IsInterfacePublic ?? defaults.IsInterfacePublic ?? true;
        
        generator.WriteDataLoaderInterface(dataLoader.InterfaceName, isInterfacePublic, kind, keyType, valueType);

        generator.WriteBeginDataLoaderClass(
            dataLoader.Name,
            dataLoader.InterfaceName,
            isPublic,
            kind,
            keyType,
            valueType);
        generator.WriteDataLoaderConstructor(dataLoader.Name, kind);
        generator.WriteDataLoaderLoadMethod(
            dataLoader.ContainingType,
            dataLoader.MethodName,
            isScoped,
            kind,
            keyType,
            valueType,
            services,
            parameterCount,
            cancelIndex);
        generator.WriteEndDataLoaderClass();
    }

    private static void InspectDataLoaderParameters(
        DataLoaderInfo dataLoader,
        ref int cancellationTokenIndex,
        Dictionary<int, string> serviceMap)
    {
        for (var i = 1; i < dataLoader.MethodSymbol.Parameters.Length; i++)
        {
            var argument = dataLoader.MethodSymbol.Parameters[i];
            var argumentType = argument.Type.ToFullyQualified();

            if (IsCancellationToken(argumentType))
            {
                if (cancellationTokenIndex != -1)
                {
                    // report error
                    return;
                }

                cancellationTokenIndex = i;
            }
            else
            {
                serviceMap[i] = argumentType;
            }
        }
    }

    private static bool IsKeysArgument(ITypeSymbol type)
        => type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1, } nt &&
            ReadOnlyList.Equals(ToTypeNameNoGenerics(nt), Ordinal);

    private static ITypeSymbol ExtractKeyType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1, } namedType &&
            ReadOnlyList.Equals(ToTypeNameNoGenerics(namedType), Ordinal))
        {
            return namedType.TypeArguments[0];
        }

        throw new InvalidOperationException();
    }

    private static bool IsCancellationToken(string typeName)
        => string.Equals(typeName, WellKnownTypes.CancellationToken) ||
            string.Equals(typeName, GlobalCancellationToken);

    private static bool IsReturnTypeDictionary(ITypeSymbol returnType, ITypeSymbol keyType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1, } namedType)
        {
            var resultType = namedType.TypeArguments[0];

            if (IsReadOnlyDictionary(resultType) &&
                resultType is INamedTypeSymbol { TypeArguments.Length: 2, } dictionaryType &&
                dictionaryType.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReturnTypeLookup(ITypeSymbol returnType, ITypeSymbol keyType)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1, } namedType)
        {
            var resultType = namedType.TypeArguments[0];

            if (ToTypeNameNoGenerics(resultType).Equals(Lookup, Ordinal) &&
                resultType is INamedTypeSymbol { TypeArguments.Length: 2, } dictionaryType &&
                dictionaryType.TypeArguments[0].Equals(keyType, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsReadOnlyDictionary(ITypeSymbol type)
    {
        if (!ToTypeNameNoGenerics(type).Equals(ReadOnlyDictionary, Ordinal))
        {
            foreach (var interfaceSymbol in type.Interfaces)
            {
                if (ToTypeNameNoGenerics(interfaceSymbol).Equals(ReadOnlyDictionary, Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    private static ITypeSymbol ExtractValueType(ITypeSymbol returnType, DataLoaderKind kind)
    {
        if (returnType is INamedTypeSymbol { TypeArguments.Length: 1, } namedType)
        {
            if (kind is DataLoaderKind.Batch or DataLoaderKind.Group &&
                namedType.TypeArguments[0] is INamedTypeSymbol { TypeArguments.Length: 2, } dict)
            {
                return dict.TypeArguments[1];
            }

            if (kind is DataLoaderKind.Cache)
            {
                return namedType.TypeArguments[0];
            }
        }

        throw new InvalidOperationException();
    }

    private static string ToTypeNameNoGenerics(ITypeSymbol typeSymbol)
        => $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}";
}