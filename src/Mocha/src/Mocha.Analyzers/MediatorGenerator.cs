using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Mocha.Analyzers.Filters;
using Mocha.Analyzers.Generators;
using Mocha.Analyzers.Inspectors;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// Provides an incremental source generator that discovers mediator message types
/// and handlers from the compilation and emits the mediator dispatch infrastructure
/// and dependency injection registrations.
/// </summary>
[Generator]
public sealed class MediatorGenerator : IIncrementalGenerator
{
    private static readonly ISyntaxInspector[] s_allInspectors =
    [
        new MultipleHandlerInterfaceInspector(),
        new HandlerInspector(),
        new NotificationHandlerInspector(),
        new MessageTypeInspector(),
        new AbstractHandlerInspector(),
        new MediatorModuleInspector()
    ];

    private static readonly ISyntaxInspector[] s_callSiteInspectors =
    [
        new CallSiteMessageTypeInspector(),
        new ImportedMediatorModuleTypeInspector()
    ];

    private static readonly ISyntaxGenerator[] s_generators =
    [
        new DependencyInjectionGenerator()
    ];

    private static readonly Dictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>> s_inspectorLookup;
    private static readonly Dictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>> s_callSiteInspectorLookup;

    private static readonly Func<SyntaxNode, bool> s_predicate;
    private static readonly Func<SyntaxNode, bool> s_callSitePredicate;

    static MediatorGenerator()
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
            .WithTrackingName("MochaSyntaxInfos")
            .Collect()
            .WithTrackingName("MochaCollectedInfos");

        var callSiteInfos = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => s_callSitePredicate(s),
                transform: static (ctx, ct) => TransformCallSite(ctx.Node, ctx.SemanticModel, ct))
            .WhereNotNull()
            .WithComparer(EqualityComparer<SyntaxInfo>.Default)
            .WithTrackingName("MochaMediatorCallSiteInfos")
            .Collect()
            .WithTrackingName("MochaMediatorCollectedCallSiteInfos");

        var assemblyName = context.CompilationProvider.Select(static (c, _) => c.AssemblyName ?? "Unknown");

        context.RegisterSourceOutput(
            assemblyName.Combine(syntaxInfos).Combine(callSiteInfos),
            static (context, source) => Execute(context, source.Left.Left, source.Left.Right, source.Right));
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
        ImmutableArray<SyntaxInfo> callSiteInfos)
    {
        var sourceFiles = PooledObjects.GetStringDictionary();
        var moduleInfo = GetModuleInfo(syntaxInfos, ModuleNameHelper.CreateModuleName(assemblyName));

        try
        {
            // Report diagnostics attached to individual SyntaxInfo entries (e.g. MO0003).
            // Reconstruct Roslyn Diagnostic objects from equatable DiagnosticInfo models.
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

            // Validate message types vs handlers (MO0001, MO0002)
            ValidateMessageHandlerPairing(context, syntaxInfos, callSiteInfos);

            // Validate call-site types vs handlers (MO0020)
            ValidateCallSiteNoHandler(context, syntaxInfos, callSiteInfos);

            foreach (var generator in s_generators)
            {
                generator.Generate(
                    context,
                    assemblyName,
                    moduleInfo.ModuleName,
                    syntaxInfos,
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

    private static MediatorModuleInfo GetModuleInfo(ImmutableArray<SyntaxInfo> syntaxInfos, string defaultModuleName)
    {
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is MediatorModuleInfo module)
            {
                return new MediatorModuleInfo(ModuleNameHelper.SanitizeIdentifier(module.ModuleName));
            }
        }

        return new MediatorModuleInfo(defaultModuleName);
    }

    private static void ValidateMessageHandlerPairing(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        ImmutableArray<SyntaxInfo> callSiteInfos)
    {
        var messageTypes = new List<MessageTypeInfo>();
        var handlers = new List<HandlerInfo>();

        foreach (var info in syntaxInfos)
        {
            if (info is MessageTypeInfo messageTypeInfo)
            {
                messageTypes.Add(messageTypeInfo);
            }
            else if (info is HandlerInfo handlerInfo && handlerInfo.Diagnostics.Count == 0)
            {
                handlers.Add(handlerInfo);
            }
        }

        if (messageTypes.Count == 0)
        {
            return;
        }

        // Collect handler message type names from imported mediator modules.
        var importedHandlerMessageTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in callSiteInfos)
        {
            if (info is ImportedMediatorModuleTypesInfo imported)
            {
                foreach (var typeName in imported.ImportedTypeNames)
                {
                    importedHandlerMessageTypes.Add(typeName);
                }
            }
        }

        // Build a lookup of handlers by message type name
        var handlersByMessageType = new Dictionary<string, List<HandlerInfo>>();
        foreach (var handler in handlers)
        {
            if (!handlersByMessageType.TryGetValue(handler.MessageTypeName, out var list))
            {
                list = new List<HandlerInfo>();
                handlersByMessageType[handler.MessageTypeName] = list;
            }

            list.Add(handler);
        }

        foreach (var messageType in messageTypes)
        {
            var location = ReconstructLocation(messageType.Location);

            if (!handlersByMessageType.TryGetValue(messageType.MessageTypeName, out var matchingHandlers)
                || matchingHandlers.Count == 0)
            {
                // If the handler exists in an imported module, skip MO0001.
                if (importedHandlerMessageTypes.Contains(messageType.MessageTypeName))
                {
                    continue;
                }

                // MO0001: Missing handler
                context.ReportDiagnostic(
                    Diagnostic.Create(Errors.MissingHandler, location, messageType.MessageTypeName));
            }
            else if (matchingHandlers.Count > 1)
            {
                // MO0002: Duplicate handler (commands and queries must have exactly one)
                // Notifications are allowed to have multiple handlers, but message types
                // tracked by MessageTypeInspector are commands/queries only (not notifications)
                var handlerNames = string.Join(", ", matchingHandlers.Select(h => h.HandlerTypeName).OrderBy(n => n));
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Errors.DuplicateHandler,
                        location,
                        messageType.MessageTypeName,
                        handlerNames));
            }
        }
    }

    private static void ValidateCallSiteNoHandler(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        ImmutableArray<SyntaxInfo> callSiteInfos)
    {
        if (callSiteInfos.Length == 0)
        {
            return;
        }

        // Build a set of handler message type names from discovered handlers.
        var handlerMessageTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in syntaxInfos)
        {
            if (info is HandlerInfo { Diagnostics.Count: 0 } handler)
            {
                handlerMessageTypes.Add(handler.MessageTypeName);
            }
        }

        // Include handler message types from imported mediator modules.
        foreach (var info in callSiteInfos)
        {
            if (info is ImportedMediatorModuleTypesInfo imported)
            {
                foreach (var typeName in imported.ImportedTypeNames)
                {
                    handlerMessageTypes.Add(typeName);
                }
            }
        }

        // Check each call-site for MediatorSend or MediatorQuery — not MediatorPublish.
        foreach (var info in callSiteInfos)
        {
            if (info is CallSiteMessageTypeInfo
                {
                    Kind: CallSiteKind.MediatorSend or CallSiteKind.MediatorQuery
                } callSite
                && !handlerMessageTypes.Contains(callSite.MessageTypeName))
            {
                var location = ReconstructLocation(callSite.Location);
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Errors.CallSiteNoHandler,
                        location,
                        callSite.MessageTypeName,
                        callSite.Kind.ToString()));
            }
        }
    }

    private static readonly Dictionary<string, DiagnosticDescriptor> s_descriptorLookup = new()
    {
        [Errors.MissingHandler.Id] = Errors.MissingHandler,
        [Errors.DuplicateHandler.Id] = Errors.DuplicateHandler,
        [Errors.AbstractHandler.Id] = Errors.AbstractHandler,
        [Errors.OpenGenericMessageType.Id] = Errors.OpenGenericMessageType,
        [Errors.MultipleHandlerInterfaces.Id] = Errors.MultipleHandlerInterfaces,
        [Errors.OpenGenericHandler.Id] = Errors.OpenGenericHandler,
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
    public static IncrementalValuesProvider<SyntaxInfo> WhereNotNull(this IncrementalValuesProvider<SyntaxInfo?> source)
        => source.Where(static t => t is not null)!;
}
