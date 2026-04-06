using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Mocha.Analyzers.FileBuilders;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.Generators;

/// <summary>
/// Provides a source generator that emits the <c>Add{Module}</c> extension method on
/// <c>IMessageBusHostBuilder</c>, registering messaging handlers and sagas into the
/// dependency injection container.
/// </summary>
public sealed class MessagingDependencyInjectionGenerator : ISyntaxGenerator
{
    /// <inheritdoc />
    public void Generate(
        SourceProductionContext context,
        string assemblyName,
        string moduleName,
        ImmutableArray<SyntaxInfo> syntaxInfos,
        Action<string, string> addSource)
    {
        var handlers = syntaxInfos
            .OfType<MessagingHandlerInfo>()
            .Where(h => h.Diagnostics.Count == 0)
            .OrderBy(h => h.OrderByKey)
            .ToList();

        var sagas = syntaxInfos
            .OfType<SagaInfo>()
            .Where(s => s.Diagnostics.Count == 0)
            .OrderBy(s => s.OrderByKey)
            .ToList();

        var contextOnlyTypes = syntaxInfos
            .OfType<ContextOnlyMessageInfo>()
            .OrderBy(c => c.OrderByKey)
            .ToList();

        if (handlers.Count == 0 && sagas.Count == 0 && contextOnlyTypes.Count == 0)
        {
            return;
        }

        // Find the module info to check for JsonContext.
        string? jsonContextTypeName = null;

        foreach (var info in syntaxInfos)
        {
            if (info is MessagingModuleInfo moduleInfo)
            {
                jsonContextTypeName = moduleInfo.JsonContextTypeName;
                break;
            }
        }

        // Collect type names imported from referenced modules — these already have
        // serializer registrations from the referenced module's Add*() method.
        var importedTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in syntaxInfos)
        {
            if (info is ImportedModuleTypesInfo imported)
            {
                foreach (var typeName in imported.ImportedTypeNames)
                {
                    importedTypeNames.Add(typeName);
                }
            }
        }

        // Collect all unique message types for the [MessagingModuleInfo] attribute.
        var allMessageTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handler in handlers)
        {
            allMessageTypeNames.Add(handler.MessageTypeName);

            if (handler.ResponseTypeName is not null)
            {
                allMessageTypeNames.Add(handler.ResponseTypeName);
            }
        }

        foreach (var contextOnly in contextOnlyTypes)
        {
            allMessageTypeNames.Add(contextOnly.MessageTypeName);
        }

        var sortedMessageTypeNames = allMessageTypeNames
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        using var builder = new MessagingDependencyInjectionFileBuilder(moduleName, assemblyName);

        builder.WriteHeader();
        builder.WriteBeginNamespace();
        builder.WriteBeginClass();
        builder.WriteBeginRegistrationMethod(sortedMessageTypeNames);

        // When JsonContext is specified, emit AOT registrations at the top of the method.
        if (jsonContextTypeName is not null)
        {
            builder.WriteSectionComment("AOT Configuration");
            builder.WriteStrictModeConfiguration();
            builder.WriteJsonTypeInfoResolverRegistration(jsonContextTypeName);

            // Collect all unique message types for pre-built serializer registration,
            // excluding types already covered by imported modules.
            var messageTypes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var handler in handlers)
            {
                if (!importedTypeNames.Contains(handler.MessageTypeName))
                {
                    messageTypes.Add(handler.MessageTypeName);
                }

                if (handler.ResponseTypeName is not null
                    && !importedTypeNames.Contains(handler.ResponseTypeName))
                {
                    messageTypes.Add(handler.ResponseTypeName);
                }
            }

            foreach (var contextOnly in contextOnlyTypes)
            {
                if (!importedTypeNames.Contains(contextOnly.MessageTypeName))
                {
                    messageTypes.Add(contextOnly.MessageTypeName);
                }
            }

            // Compute enclosed types per message type (pre-sorted by specificity).
            var enclosedTypesMap = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            // Build a lookup from message type name to its full hierarchy.
            var hierarchyLookup = new Dictionary<string, ImmutableEquatableArray<string>>(StringComparer.Ordinal);
            foreach (var handler in handlers)
            {
                if (!hierarchyLookup.ContainsKey(handler.MessageTypeName))
                {
                    hierarchyLookup[handler.MessageTypeName] = handler.MessageTypeHierarchy;
                }
            }

            foreach (var contextOnly in contextOnlyTypes)
            {
                if (!hierarchyLookup.ContainsKey(contextOnly.MessageTypeName))
                {
                    hierarchyLookup[contextOnly.MessageTypeName] = contextOnly.MessageTypeHierarchy;
                }
            }

            foreach (var messageTypeName in messageTypes)
            {
                // Start with the type itself.
                var enclosed = new List<string> { messageTypeName };

                // Filter hierarchy to registered types only (exclude framework types).
                if (hierarchyLookup.TryGetValue(messageTypeName, out var hierarchy))
                {
                    foreach (var typeName in hierarchy)
                    {
                        if (messageTypes.Contains(typeName) && !enclosed.Contains(typeName))
                        {
                            enclosed.Add(typeName);
                        }
                    }
                }

                // Sort by specificity: most specific first.
                // Type A is "more specific" than type B if B appears in A's hierarchy.
                enclosed.Sort((a, b) =>
                {
                    int ScoreOf(string typeName)
                    {
                        if (!hierarchyLookup.TryGetValue(typeName, out var h))
                        {
                            return 0;
                        }

                        return enclosed.Count(other => other != typeName && h.Contains(other));
                    }

                    return ScoreOf(b).CompareTo(ScoreOf(a));
                });

                enclosedTypesMap[messageTypeName] = enclosed;
            }

            builder.WriteSectionComment("Message Type Serializers");

            foreach (var messageType in messageTypes.OrderBy(t => t, StringComparer.Ordinal))
            {
                enclosedTypesMap.TryGetValue(messageType, out var enclosedTypes);
                builder.WriteMessageConfiguration(messageType, jsonContextTypeName, enclosedTypes);
            }

            if (sagas.Count > 0)
            {
                builder.WriteSectionComment("Saga Configuration");

                foreach (var saga in sagas)
                {
                    builder.WriteSagaConfiguration(saga.SagaTypeName, saga.StateTypeName, jsonContextTypeName);
                }
            }
        }

        var batchHandlers = handlers
            .Where(h => h.Kind == MessagingHandlerKind.Batch)
            .OrderBy(h => h.HandlerTypeName)
            .ToList();

        if (batchHandlers.Count > 0)
        {
            builder.WriteSectionComment("Batch Handlers");

            foreach (var handler in batchHandlers)
            {
                builder.WriteHandlerRegistration(handler);
            }
        }

        var consumers = handlers
            .Where(h => h.Kind == MessagingHandlerKind.Consumer)
            .OrderBy(h => h.HandlerTypeName)
            .ToList();

        if (consumers.Count > 0)
        {
            builder.WriteSectionComment("Consumers");

            foreach (var handler in consumers)
            {
                builder.WriteHandlerRegistration(handler);
            }
        }

        var requestHandlers = handlers
            .Where(h => h.Kind is MessagingHandlerKind.RequestResponse or MessagingHandlerKind.Send)
            .OrderBy(h => h.HandlerTypeName)
            .ToList();

        if (requestHandlers.Count > 0)
        {
            builder.WriteSectionComment("Request Handlers");

            foreach (var handler in requestHandlers)
            {
                builder.WriteHandlerRegistration(handler);
            }
        }

        var eventHandlers = handlers
            .Where(h => h.Kind == MessagingHandlerKind.Event)
            .OrderBy(h => h.HandlerTypeName)
            .ToList();

        if (eventHandlers.Count > 0)
        {
            builder.WriteSectionComment("Event Handlers");

            foreach (var handler in eventHandlers)
            {
                builder.WriteHandlerRegistration(handler);
            }
        }

        if (sagas.Count > 0)
        {
            builder.WriteSectionComment("Sagas");

            foreach (var saga in sagas)
            {
                builder.WriteSagaRegistration(saga);
            }
        }

        builder.WriteEndRegistrationMethod();
        builder.WriteEndClass();
        builder.WriteEndNamespace();

        addSource(builder.HintName + ".g.cs", builder.ToSourceText());
    }
}
