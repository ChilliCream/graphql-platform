using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    private static readonly ISyntaxGenerator[] s_generators =
    [
        new MessagingDependencyInjectionGenerator()
    ];

    private static readonly Dictionary<SyntaxKind, ImmutableArray<ISyntaxInspector>> s_inspectorLookup;

    private static readonly Func<SyntaxNode, bool> s_predicate;

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

        var assemblyName = context.CompilationProvider.Select(static (c, _) => c.AssemblyName ?? "Unknown");

        context.RegisterSourceOutput(
            assemblyName.Combine(syntaxInfos),
            static (context, source) => Execute(context, source.Left, source.Right));
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

    private static void Execute(
        SourceProductionContext context,
        string assemblyName,
        ImmutableArray<SyntaxInfo> syntaxInfos)
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

    private static MessagingModuleInfo GetModuleInfo(ImmutableArray<SyntaxInfo> syntaxInfos, string defaultModuleName)
    {
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is MessagingModuleInfo module)
            {
                return new MessagingModuleInfo(ModuleNameHelper.SanitizeIdentifier(module.ModuleName));
            }
        }

        return new MessagingModuleInfo(defaultModuleName);
    }

    private static void ValidateRequestHandlerPairing(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        var requestHandlers = new List<MessagingHandlerInfo>();

        foreach (var info in syntaxInfos)
        {
            if (info is MessagingHandlerInfo handlerInfo
                && handlerInfo.Diagnostics.Count == 0
                && (handlerInfo.Kind == MessagingHandlerKind.Send
                    || handlerInfo.Kind == MessagingHandlerKind.RequestResponse))
            {
                requestHandlers.Add(handlerInfo);
            }
        }

        if (requestHandlers.Count == 0)
        {
            return;
        }

        // Group by message type to detect duplicates
        var handlersByMessageType = new Dictionary<string, List<MessagingHandlerInfo>>();
        foreach (var handler in requestHandlers)
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

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Errors.DuplicateRequestHandler,
                        Location.None,
                        firstHandler.MessageTypeName,
                        handlerNames));
            }
        }
    }

    private static readonly Dictionary<string, DiagnosticDescriptor> s_descriptorLookup = new()
    {
        [Errors.MissingRequestHandler.Id] = Errors.MissingRequestHandler,
        [Errors.DuplicateRequestHandler.Id] = Errors.DuplicateRequestHandler,
        [Errors.OpenGenericMessagingHandler.Id] = Errors.OpenGenericMessagingHandler,
        [Errors.AbstractMessagingHandler.Id] = Errors.AbstractMessagingHandler,
        [Errors.SagaMissingParameterlessConstructor.Id] = Errors.SagaMissingParameterlessConstructor
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
