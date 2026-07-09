using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Generators;
using Mocha.Analyzers.Inspectors;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Provides an incremental source generator that discovers MessageBus handlers
/// and sagas from the compilation and emits the dependency injection registrations.
/// </summary>
[Generator]
public sealed class MessagingGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] s_allInspectors =
    [
        new MessagingHandlerInspector(),
        new AbstractMessagingHandlerInspector(),
        new MessagingModuleInspector(),
        new SagaInspector()
    ];

    private static readonly ISyntaxInspector[] s_callSiteInspectors =
    [
        new CallSiteMessageTypeInspector(),
        new ImportedModuleTypeInspector(),
        new AddMessageDeclarationInspector()
    ];

    private static readonly ISyntaxGenerator[] s_generators =
    [
        new MessagingDependencyInjectionGenerator()
    ];

    private static readonly Dictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>> s_inspectorLookup;
    private static readonly Dictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>> s_callSiteInspectorLookup;

    private static readonly Func<SyntaxNode, bool> s_predicate;
    private static readonly Func<SyntaxNode, bool> s_callSitePredicate;

    static MessagingGenerator()
    {
        var filterBuilder = new SyntaxFilterBuilder();
        var inspectorLookup = new Dictionary<SyntaxKind, List<ISyntaxInspector>>();

        foreach (var inspector in s_allInspectors)
        {
            filterBuilder.AddRange(inspector.Filters);

            foreach (var supportedKind in inspector.SupportedKinds)
            {
                if (!inspectorLookup.TryGetValue(supportedKind, out var inspectors))
                {
                    inspectors = [];
                    inspectorLookup[supportedKind] = inspectors;
                }

                inspectors.Add(inspector);
            }
        }

        s_predicate = filterBuilder.Build();
        s_inspectorLookup = new Dictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>>();

        foreach (var kvp in inspectorLookup)
        {
            s_inspectorLookup[kvp.Key] = kvp.Value.ToImmutableArray();
        }

        // Build call-site filter + inspector lookup.
        var callSiteFilterBuilder = new SyntaxFilterBuilder();
        var callSiteInspectorLookup = new Dictionary<SyntaxKind, List<ISyntaxInspector>>();

        foreach (var inspector in s_callSiteInspectors)
        {
            callSiteFilterBuilder.AddRange(inspector.Filters);

            foreach (var supportedKind in inspector.SupportedKinds)
            {
                if (!callSiteInspectorLookup.TryGetValue(supportedKind, out var inspectors))
                {
                    inspectors = [];
                    callSiteInspectorLookup[supportedKind] = inspectors;
                }

                inspectors.Add(inspector);
            }
        }

        s_callSitePredicate = callSiteFilterBuilder.Build();
        s_callSiteInspectorLookup = new Dictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>>();

        foreach (var kvp in callSiteInspectorLookup)
        {
            s_callSiteInspectorLookup[kvp.Key] = kvp.Value.ToImmutableArray();
        }
    }

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxInfos = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => s_predicate(s),
                transform: static (ctx, ct) => Transform(ctx.Node, ctx.SemanticModel, ct))
            .WhereNotNull()
            .WithComparer(EqualityComparer<SyntaxInfo>.Default)
            .WithTrackingName("MochaMessagingSyntaxInfos")
            .Collect()
            .WithTrackingName("MochaMessagingCollectedInfos");

        var callSiteInfos = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => s_callSitePredicate(s),
                transform: static (ctx, ct) => TransformCallSite(ctx.Node, ctx.SemanticModel, ct))
            .WhereNotNull()
            .WithComparer(EqualityComparer<SyntaxInfo>.Default)
            .WithTrackingName("MochaCallSiteInfos")
            .Collect()
            .WithTrackingName("MochaCollectedCallSiteInfos");

        var assemblyName = context.CompilationProvider.Select(static (c, _) => c.AssemblyName ?? "Unknown");

        var isAotPublish = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
        {
            options.GlobalOptions.TryGetValue("build_property.PublishAot", out var publishAot);
            return string.Equals(publishAot, "true", StringComparison.OrdinalIgnoreCase);
        });

        var sourceMetadataOptions = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
        {
            options.GlobalOptions.TryGetValue("build_property.MochaEmitSourceMetadata", out var emit);
            options.GlobalOptions.TryGetValue("build_property.ProjectDir", out var projectDir);
            options.GlobalOptions.TryGetValue("build_property.RepositoryUrl", out var repositoryUrl);
            options.GlobalOptions.TryGetValue("build_property.SourceRevisionId", out var commit);
            return new SourceMetadataOptionsInfo(
                !string.Equals(emit, "false", StringComparison.OrdinalIgnoreCase),
                string.IsNullOrWhiteSpace(projectDir) ? null : projectDir,
                string.IsNullOrWhiteSpace(repositoryUrl) ? null : repositoryUrl,
                string.IsNullOrWhiteSpace(commit) ? null : commit);
        });

        var jsonSerializableInfos = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsJsonSerializableAttributeCandidate(s),
                transform: static (ctx, ct) => ExtractJsonSerializableInfo(ctx.Node, ctx.SemanticModel, ct))
            .WhereNotNull()
            .WithComparer(EqualityComparer<JsonSerializableAttributeInfo>.Default)
            .Collect()
            .WithTrackingName("MochaJsonSerializableInfos");

        // Keep JsonContext data syntax-driven instead of flowing CompilationProvider into this path.
        var jsonContextInfo = syntaxInfos
            .Combine(jsonSerializableInfos)
            .Select(static (source, _) => BuildJsonContextInfo(source.Left, source.Right));

        // Aggregate the per-type declaration metadata (doc + location) captured cross-file from every discovery
        // source: handler infos (message + response types), dispatch and explicit AddMessage call sites, and
        // the weakest source, the [JsonSerializable] list. Entries are merged by fully qualified type name so a
        // type declared in another assembly still resolves whenever any source in this compilation observes it.
        var messageDeclarations = jsonContextInfo
            .Combine(syntaxInfos)
            .Combine(callSiteInfos)
            .Select(static (source, _) =>
                BuildMessageDeclarations(source.Left.Left, source.Left.Right, source.Right))
            .WithTrackingName("MochaMessageDeclarations");

        context.RegisterSourceOutput(
            assemblyName
                .Combine(syntaxInfos)
                .Combine(callSiteInfos)
                .Combine(isAotPublish)
                .Combine(jsonContextInfo)
                .Combine(sourceMetadataOptions)
                .Combine(messageDeclarations),
            static (context, source) => Execute(
                context,
                source.Left.Left.Left.Left.Left.Left,
                source.Left.Left.Left.Left.Left.Right,
                source.Left.Left.Left.Left.Right,
                source.Left.Left.Left.Right,
                source.Left.Left.Right,
                source.Left.Right,
                source.Right));
    }

    private static SyntaxInfo? Transform(
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (!s_inspectorLookup.TryGetValue(node.Kind(), out var inspectors))
        {
            return null;
        }

        var knownTypeSymbols = KnownTypeSymbols.GetOrCreate(semanticModel.Compilation);

        foreach (var inspector in inspectors)
        {
            if (inspector.TryHandle(knownTypeSymbols, node, semanticModel, cancellationToken, out var syntaxInfo))
            {
                return syntaxInfo;
            }
        }

        return null;
    }

    private static SyntaxInfo? TransformCallSite(
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (!s_callSiteInspectorLookup.TryGetValue(node.Kind(), out var inspectors))
        {
            return null;
        }

        var knownTypeSymbols = KnownTypeSymbols.GetOrCreate(semanticModel.Compilation);

        foreach (var inspector in inspectors)
        {
            if (inspector.TryHandle(knownTypeSymbols, node, semanticModel, cancellationToken, out var syntaxInfo))
            {
                return syntaxInfo;
            }
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext context,
        string assemblyName,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        ImmutableArray<SyntaxInfo> callSiteInfos,
        bool isAotPublish,
        JsonContextInfo jsonContextInfo,
        SourceMetadataOptionsInfo sourceMetadataOptions,
        MessageDeclarationsInfo messageDeclarations)
    {
        var sourceFiles = PooledObjects.GetStringDictionary();
        var moduleInfo = GetModuleInfo(syntaxInfos, ModuleNameHelper.CreateModuleName(assemblyName));

        try
        {
            // Report diagnostics attached to individual SyntaxInfo entries (e.g. MO0012, MO0013, MO0014).
            foreach (var syntaxInfo in syntaxInfos)
            {
                if (syntaxInfo.Diagnostics.Count > 0)
                {
                    foreach (var diagInfo in syntaxInfo.Diagnostics)
                    {
                        context.ReportDiagnostic(ReconstructDiagnostic(diagInfo));
                    }
                }
            }

            // Validate request handler pairing (MO0011)
            ValidateRequestHandlerPairing(context, syntaxInfos);

            // Validate AOT JsonSerializerContext coverage (MO0015, MO0016)
            ValidateAotJsonContext(context, syntaxInfos, callSiteInfos, moduleInfo, isAotPublish, jsonContextInfo);

            // Validate call-site types against JsonSerializerContext (MO0018)
            ValidateCallSiteJsonContext(context, callSiteInfos, isAotPublish, jsonContextInfo);

            // Extract context-only message types from JsonSerializerContext (types without handlers).
            var augmentedInfos = ExtractContextOnlyTypes(syntaxInfos, jsonContextInfo);
            augmentedInfos = augmentedInfos.Add(new AotPublishInfo(isAotPublish));
            augmentedInfos = augmentedInfos.Add(sourceMetadataOptions);

            if (messageDeclarations.Declarations.Count > 0)
            {
                augmentedInfos = augmentedInfos.Add(messageDeclarations);
            }

            // Pass the full set of JsonContext-serializable type names so the DI generator
            // can restrict serializer registrations to types that are actually in the context.
            if (jsonContextInfo.SerializableTypes.Count > 0)
            {
                var serializableTypeNames = new ImmutableEquatableArray<string>(
                    jsonContextInfo.SerializableTypes.Select(t => t.TypeName));
                augmentedInfos = augmentedInfos.Add(new JsonContextSerializableTypesInfo(serializableTypeNames));
            }

            // Include ImportedModuleTypesInfo entries so the DI generator can skip
            // registrations for types already covered by module methods invoked in this compilation.
            var importedModuleInfos = callSiteInfos.OfType<ImportedModuleTypesInfo>().ToImmutableArray();

            if (importedModuleInfos.Length > 0)
            {
                augmentedInfos = augmentedInfos.AddRange(importedModuleInfos.CastArray<SyntaxInfo>());
            }

            foreach (var generator in s_generators)
            {
                generator.Generate(
                    context,
                    assemblyName,
                    moduleInfo.ModuleName,
                    augmentedInfos,
                    AddSource);
            }

            foreach (var sourceFile in sourceFiles)
            {
                context.AddSource(sourceFile.Key, SourceText.From(sourceFile.Value, Encoding.UTF8));
            }
        }
        finally
        {
            PooledObjects.Return(sourceFiles);
        }

        void AddSource(string fileName, string sourceText)
        {
            sourceFiles[fileName] = sourceText;
        }
    }

    private static MessagingModuleInfo GetModuleInfo(ImmutableArray<SyntaxInfo> syntaxInfos, string defaultModuleName)
    {
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is MessagingModuleInfo module)
            {
                return new MessagingModuleInfo(
                    ModuleNameHelper.SanitizeIdentifier(module.ModuleName),
                    module.JsonContextTypeName);
            }
        }

        return new MessagingModuleInfo(defaultModuleName);
    }

    private static bool IsJsonSerializableAttributeCandidate(SyntaxNode node)
        => node is AttributeSyntax attribute
        && GetUnqualifiedName(attribute.Name) is "JsonSerializable" or "JsonSerializableAttribute";

    private static string? GetUnqualifiedName(NameSyntax name)
        => name switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            QualifiedNameSyntax qualified => GetUnqualifiedName(qualified.Right),
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.Identifier.Text,
            _ => null
        };

    private static JsonSerializableAttributeInfo? ExtractJsonSerializableInfo(
        SyntaxNode node,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (node is not AttributeSyntax attribute
            || attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not TypeOfExpressionSyntax typeOf)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var contextDeclaration = attribute.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (contextDeclaration is null)
        {
            return null;
        }

        var contextSymbol = semanticModel.GetDeclaredSymbol(contextDeclaration, cancellationToken);
        if (contextSymbol is null)
        {
            return null;
        }

        if (semanticModel.GetTypeInfo(typeOf.Type, cancellationToken).Type is not INamedTypeSymbol serializableType)
        {
            return null;
        }

        return new JsonSerializableAttributeInfo(
            contextSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            CreateJsonSerializableTypeInfo(
                serializableType,
                attribute.GetLocation().ToDeclarationLocationInfo(),
                cancellationToken));
    }

    private static JsonContextInfo BuildJsonContextInfo(
        ImmutableArray<SyntaxInfo> syntaxInfos,
        ImmutableArray<JsonSerializableAttributeInfo> jsonSerializableInfos)
    {
        // Find the module info to get the JsonContext type name.
        string? jsonContextTypeName = null;

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is MessagingModuleInfo module)
            {
                jsonContextTypeName = module.JsonContextTypeName;
                break;
            }
        }

        if (jsonContextTypeName is null)
        {
            return new JsonContextInfo(null, ImmutableEquatableArray<JsonSerializableTypeInfo>.Empty);
        }

        var serializableTypes = new List<JsonSerializableTypeInfo>();

        foreach (var info in jsonSerializableInfos)
        {
            if (info.JsonContextTypeName == jsonContextTypeName)
            {
                serializableTypes.Add(info.SerializableType);
            }
        }

        return new JsonContextInfo(
            jsonContextTypeName,
            new ImmutableEquatableArray<JsonSerializableTypeInfo>(serializableTypes));
    }

    private static MessageDeclarationsInfo BuildMessageDeclarations(
        JsonContextInfo jsonContextInfo,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        ImmutableArray<SyntaxInfo> callSiteInfos)
    {
        // Aggregation always runs: message registration and SourceMetadata emission no longer require a
        // JsonSerializerContext, so every discovered declared message type must flow to the DI generator.

        // Merge entries by fully qualified type name, deduplicating across sources and across partial parts.
        // Every source resolves the same closed symbol, so overlapping entries collapse to an identical value.
        var byTypeName = new Dictionary<string, DeclaredTypeInfo>(StringComparer.Ordinal);

        // Type names registered by an explicit user AddMessage<T>() call. The user's own call already emits
        // AddMessage, so the generated module contributes only the descriptor callback for these types.
        var explicitAddMessageTypeNames = new HashSet<string>(StringComparer.Ordinal);

        // Source A: handler message and response declarations.
        foreach (var info in syntaxInfos)
        {
            if (info is MessagingHandlerInfo handler)
            {
                AddDeclaration(byTypeName, handler.DeclaredMessageType);
                AddDeclaration(byTypeName, handler.DeclaredResponseType);
            }
        }

        // Sources B and C: dispatch call sites and explicit AddMessage registrations.
        foreach (var info in callSiteInfos)
        {
            if (info is CallSiteMessageTypeInfo callSite)
            {
                AddDeclaration(byTypeName, callSite.DeclaredMessageType);
                AddDeclaration(byTypeName, callSite.DeclaredResponseType);
            }
            else if (info is AddMessageDeclarationInfo addMessage)
            {
                AddDeclaration(byTypeName, addMessage.DeclaredMessageType);
                explicitAddMessageTypeNames.Add(addMessage.DeclaredMessageType.TypeName);
            }
        }

        // Weakest source: the [JsonSerializable] list (AOT-only, requires a context).
        foreach (var serializableType in jsonContextInfo.SerializableTypes)
        {
            AddDeclaration(byTypeName, serializableType.Declaration);
        }

        var survivors = byTypeName.Values
            .OrderBy(d => d.TypeName, StringComparer.Ordinal)
            .ToList();

        var explicitNames = explicitAddMessageTypeNames
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        return new MessageDeclarationsInfo(
            new ImmutableEquatableArray<DeclaredTypeInfo>(survivors),
            new ImmutableEquatableArray<string>(explicitNames));
    }

    private static void AddDeclaration(
        Dictionary<string, DeclaredTypeInfo> byTypeName,
        DeclaredTypeInfo? declared)
    {
        // Metadata-only types (declared in another assembly) carry no captured declaration.
        if (declared is null)
        {
            return;
        }

        if (!byTypeName.TryGetValue(declared.TypeName, out var existing))
        {
            byTypeName[declared.TypeName] = declared;
            return;
        }

        byTypeName[declared.TypeName] = MergeDeclarations(existing, declared);
    }

    private static DeclaredTypeInfo MergeDeclarations(DeclaredTypeInfo first, DeclaredTypeInfo second)
    {
        // XmlDocumentation is the first non-null; partial parts carry identical symbol-level doc.
        var xmlDocumentation = first.XmlDocumentation ?? second.XmlDocumentation;

        // Prefer the entry with the ordinally smallest location; a null location always loses.
        var location = LocationInfo.Min(first.Location, second.Location);

        return new DeclaredTypeInfo(first.TypeName, xmlDocumentation, location);
    }

    private static JsonSerializableTypeInfo CreateJsonSerializableTypeInfo(
        INamedTypeSymbol serializableType,
        LocationInfo? location,
        CancellationToken cancellationToken)
    {
        var typeName = serializableType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeNamespace = serializableType.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        // Walk the full type hierarchy (base types + interfaces), same as MessagingHandlerInspector.
        var hierarchy = new List<string>();
        var currentBase = serializableType.BaseType;

        while (currentBase is not null && currentBase.SpecialType != SpecialType.System_Object)
        {
            hierarchy.Add(currentBase.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            currentBase = currentBase.BaseType;
        }

        foreach (var iface in serializableType.AllInterfaces)
        {
            hierarchy.Add(iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        // Capture the declaration metadata (doc + location) cross-file from the resolved symbol so
        // context-only types (serializable types with no handler) still carry their own declaration.
        // This is stale only in the IDE's incremental cache; every real compiler invocation runs fresh.
        var declaration = serializableType.ToDeclaredTypeInfo(cancellationToken);

        return new JsonSerializableTypeInfo(
            typeName,
            typeNamespace,
            new ImmutableEquatableArray<string>(hierarchy),
            location,
            declaration);
    }

    private sealed record JsonSerializableAttributeInfo(
        string JsonContextTypeName,
        JsonSerializableTypeInfo SerializableType);

    private static void ValidateRequestHandlerPairing(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        var requestHandlers = new List<MessagingHandlerInfo>();

        foreach (var info in syntaxInfos)
        {
            if (info is MessagingHandlerInfo
                {
                    Diagnostics.Count: 0,
                    Kind: MessagingHandlerKind.Send or MessagingHandlerKind.RequestResponse
                } handlerInfo)
            {
                requestHandlers.Add(handlerInfo);
            }
        }

        if (requestHandlers.Count == 0)
        {
            return;
        }

        // Dedupe handler infos by handler type name so a handler declared across multiple
        // partial declaration parts (e.g. restating the handler interface on each part)
        // counts once, matching the emission-side deduplication in MessagingDependencyInjectionGenerator.
        var dedupedRequestHandlers = DeduplicationHelper.SelectRepresentatives(
            requestHandlers,
            h => h.HandlerTypeName,
            h => h.XmlDocumentation,
            h => h.Location);

        // Group by message type to detect duplicates
        var handlersByMessageType = new Dictionary<string, List<MessagingHandlerInfo>>();
        foreach (var handler in dedupedRequestHandlers)
        {
            if (!handlersByMessageType.TryGetValue(handler.MessageTypeName, out var list))
            {
                list = new List<MessagingHandlerInfo>();
                handlersByMessageType[handler.MessageTypeName] = list;
            }

            list.Add(handler);
        }

        foreach (var kvp in handlersByMessageType)
        {
            if (kvp.Value.Count > 1)
            {
                // MO0011: Duplicate request handler
                var handlerNames = string.Join(", ", kvp.Value.Select(h => h.HandlerTypeName).OrderBy(n => n));
                var firstHandler = kvp.Value[0];
                var diagnosticLocation = ReconstructLocation(firstHandler.Location);

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Errors.DuplicateRequestHandler,
                        diagnosticLocation,
                        firstHandler.MessageTypeName,
                        handlerNames));
            }
        }
    }

    private static void ValidateAotJsonContext(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        ImmutableArray<SyntaxInfo> callSiteInfos,
        MessagingModuleInfo moduleInfo,
        bool isAotPublish,
        JsonContextInfo jsonContextInfo)
    {
        if (!isAotPublish)
        {
            return;
        }

        // Collect type names imported from referenced modules via [MessagingModuleInfo].
        var importedTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in callSiteInfos)
        {
            if (info is ImportedModuleTypesInfo imported)
            {
                foreach (var typeName in imported.ImportedTypeNames)
                {
                    importedTypes.Add(typeName);
                }
            }
        }

        if (jsonContextInfo.JsonContextTypeName is null)
        {
            // Only fire MO0015 if this module has handlers or sagas that need serialization coverage
            // AND those types are not already covered by imported modules.
            var requiredTypes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var syntaxInfo in syntaxInfos)
            {
                if (syntaxInfo is MessagingHandlerInfo { Diagnostics.Count: 0 } handler)
                {
                    requiredTypes.Add(handler.MessageTypeName);

                    if (handler.ResponseTypeName is not null)
                    {
                        requiredTypes.Add(handler.ResponseTypeName);
                    }
                }
                else if (syntaxInfo is SagaInfo { Diagnostics.Count: 0 } saga)
                {
                    requiredTypes.Add(saga.StateTypeName);
                }
            }

            AddRequiredCallSiteTypes(callSiteInfos, requiredTypes);

            // If all required types are covered by imported modules, no local JsonContext is needed.
            if (requiredTypes.Count == 0 || requiredTypes.IsSubsetOf(importedTypes))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Errors.MissingJsonSerializerContext,
                    Location.None,
                    moduleInfo.ModuleName));

            return;
        }

        // Build the set of covered types from the local JsonContext
        // plus types imported from referenced modules.
        var coveredTypes = new HashSet<string>(importedTypes, StringComparer.Ordinal);

        foreach (var serializableType in jsonContextInfo.SerializableTypes)
        {
            coveredTypes.Add(serializableType.TypeName);
        }

        // Collect all required message types.
        var requiredMessageTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is MessagingHandlerInfo { Diagnostics.Count: 0 } handler)
            {
                requiredMessageTypes.Add(handler.MessageTypeName);

                if (handler.ResponseTypeName is not null)
                {
                    requiredMessageTypes.Add(handler.ResponseTypeName);
                }
            }
            else if (syntaxInfo is SagaInfo { Diagnostics.Count: 0 } saga)
            {
                requiredMessageTypes.Add(saga.StateTypeName);
            }
        }

        // Emit MO0016 for each required type not covered by the JsonSerializerContext
        // or imported modules.
        foreach (var requiredType in requiredMessageTypes.OrderBy(t => t, StringComparer.Ordinal))
        {
            if (!coveredTypes.Contains(requiredType))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Errors.MissingJsonSerializable,
                        Location.None,
                        requiredType,
                        jsonContextInfo.JsonContextTypeName));
            }
        }
    }

    private static void AddRequiredCallSiteTypes(
        ImmutableArray<SyntaxInfo> callSiteInfos,
        HashSet<string> requiredTypes)
    {
        foreach (var info in callSiteInfos)
        {
            if (info is CallSiteMessageTypeInfo callSite
                && callSite.Kind is not (CallSiteKind.MediatorSend or CallSiteKind.MediatorQuery or CallSiteKind.MediatorPublish))
            {
                requiredTypes.Add(callSite.MessageTypeName);

                if (callSite.ResponseTypeName is not null)
                {
                    requiredTypes.Add(callSite.ResponseTypeName);
                }
            }
        }
    }

    private static void ValidateCallSiteJsonContext(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> callSiteInfos,
        bool isAotPublish,
        JsonContextInfo jsonContextInfo)
    {
        if (!isAotPublish || callSiteInfos.Length == 0)
        {
            return;
        }

        // Build the set of covered types from the local JsonContext
        // plus types imported from referenced modules.
        var coveredTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var serializableType in jsonContextInfo.SerializableTypes)
        {
            coveredTypes.Add(serializableType.TypeName);
        }

        foreach (var info in callSiteInfos)
        {
            if (info is ImportedModuleTypesInfo imported)
            {
                foreach (var typeName in imported.ImportedTypeNames)
                {
                    coveredTypes.Add(typeName);
                }
            }
        }

        // If there are no covered types and no local context, MO0015 handles the missing context case.
        if (coveredTypes.Count == 0 && jsonContextInfo.JsonContextTypeName is null)
        {
            return;
        }

        // Determine the context name for the diagnostic message.
        // If no local context, use a generic description.
        var contextDisplayName = jsonContextInfo.JsonContextTypeName ?? "any registered module";

        // Emit MO0018 for each call-site type not covered by the JsonSerializerContext
        // or imported module registrations. Only check messaging call sites - mediator
        // dispatch is in-process and does not require JSON serialization.
        foreach (var info in callSiteInfos)
        {
            if (info is CallSiteMessageTypeInfo callSite
                && callSite.Kind is not (CallSiteKind.MediatorSend or CallSiteKind.MediatorQuery or CallSiteKind.MediatorPublish))
            {
                if (!coveredTypes.Contains(callSite.MessageTypeName))
                {
                    var location = ReconstructLocation(callSite.Location);
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Errors.CallSiteTypeNotInJsonContext,
                            location,
                            callSite.MessageTypeName,
                            callSite.Kind.ToString(),
                            contextDisplayName));
                }

                if (callSite.ResponseTypeName is not null
                    && !coveredTypes.Contains(callSite.ResponseTypeName))
                {
                    var location = ReconstructLocation(callSite.Location);
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Errors.CallSiteTypeNotInJsonContext,
                            location,
                            callSite.ResponseTypeName,
                            callSite.Kind.ToString(),
                            contextDisplayName));
                }
            }
        }
    }

    private static ImmutableArray<SyntaxInfo> ExtractContextOnlyTypes(
        ImmutableArray<SyntaxInfo> syntaxInfos,
        JsonContextInfo jsonContextInfo)
    {
        if (jsonContextInfo.JsonContextTypeName is null || jsonContextInfo.SerializableTypes.Count == 0)
        {
            return syntaxInfos;
        }

        // Collect handler message type names and response type names.
        var handlerTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in syntaxInfos)
        {
            if (info is MessagingHandlerInfo { Diagnostics.Count: 0 } handler)
            {
                handlerTypeNames.Add(handler.MessageTypeName);

                if (handler.ResponseTypeName is not null)
                {
                    handlerTypeNames.Add(handler.ResponseTypeName);
                }
            }
        }

        // Collect saga state type names.
        var sagaStateTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in syntaxInfos)
        {
            if (info is SagaInfo { Diagnostics.Count: 0 } saga)
            {
                sagaStateTypeNames.Add(saga.StateTypeName);
            }
        }

        // Find context-only types from the pre-extracted JsonContext data.
        var contextOnlyInfos = new List<SyntaxInfo>();

        foreach (var serializableType in jsonContextInfo.SerializableTypes)
        {
            // Skip types already covered by handlers or sagas.
            if (handlerTypeNames.Contains(serializableType.TypeName)
                || sagaStateTypeNames.Contains(serializableType.TypeName))
            {
                continue;
            }

            contextOnlyInfos.Add(new ContextOnlyMessageInfo(
                serializableType.TypeName,
                serializableType.TypeNamespace,
                serializableType.TypeHierarchy,
                serializableType.AttributeLocation));
        }

        if (contextOnlyInfos.Count == 0)
        {
            return syntaxInfos;
        }

        return syntaxInfos.AddRange(contextOnlyInfos);
    }

    private static readonly Dictionary<string, DiagnosticDescriptor> s_descriptorLookup = new()
    {
        [Errors.DuplicateRequestHandler.Id] = Errors.DuplicateRequestHandler,
        [Errors.OpenGenericMessagingHandler.Id] = Errors.OpenGenericMessagingHandler,
        [Errors.AbstractMessagingHandler.Id] = Errors.AbstractMessagingHandler,
        [Errors.SagaMissingParameterlessConstructor.Id] = Errors.SagaMissingParameterlessConstructor,
        [Errors.MissingJsonSerializerContext.Id] = Errors.MissingJsonSerializerContext,
        [Errors.MissingJsonSerializable.Id] = Errors.MissingJsonSerializable,
        [Errors.CallSiteTypeNotInJsonContext.Id] = Errors.CallSiteTypeNotInJsonContext,
        [Errors.CallSiteNoHandler.Id] = Errors.CallSiteNoHandler
    };

    private static Diagnostic ReconstructDiagnostic(DiagnosticInfo info)
    {
        var descriptor = s_descriptorLookup[info.DescriptorId];
        var location = ReconstructLocation(info.Location);
        var args = new object[info.MessageArgs.Count];
        for (var i = 0; i < info.MessageArgs.Count; i++)
        {
            args[i] = info.MessageArgs[i];
        }

        return Diagnostic.Create(descriptor, location, args);
    }

    private static Location ReconstructLocation(LocationInfo? locationInfo)
    {
        if (locationInfo is null)
        {
            return Location.None;
        }

        return Location.Create(
            locationInfo.FilePath,
            default,
            new LinePositionSpan(
                new LinePosition(locationInfo.StartLine, locationInfo.StartColumn),
                new LinePosition(locationInfo.EndLine, locationInfo.EndColumn)));
    }
}

file static class Extensions
{
    public static IncrementalValuesProvider<T> WhereNotNull<T>(this IncrementalValuesProvider<T?> source)
        where T : class
        => source.Where(static t => t is not null)!;
}
