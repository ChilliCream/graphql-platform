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
        var importedSagaTypeNames = new HashSet<string>(StringComparer.Ordinal);
        var importedHandlerTypeNames = new HashSet<string>(StringComparer.Ordinal);

        // Collect type names that are actually declared in the local JsonSerializerContext.
        var jsonContextTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in syntaxInfos)
        {
            if (info is ImportedModuleTypesInfo imported)
            {
                foreach (var typeName in imported.ImportedTypeNames)
                {
                    importedTypeNames.Add(typeName);
                }

                foreach (var typeName in imported.ImportedSagaTypeNames)
                {
                    importedSagaTypeNames.Add(typeName);
                }

                foreach (var typeName in imported.ImportedHandlerTypeNames)
                {
                    importedHandlerTypeNames.Add(typeName);
                }
            }
            else if (info is JsonContextSerializableTypesInfo jsonContextTypes)
            {
                foreach (var typeName in jsonContextTypes.TypeNames)
                {
                    jsonContextTypeNames.Add(typeName);
                }
            }
        }

        // Collect all unique message types that this module actually registers
        // serializers for via AddMessageConfiguration. Only types in the local
        // JsonSerializerContext (excluding imports) qualify. This set is used for both
        // the [MessagingModuleInfo] attribute and the actual serializer registrations.
        var messageTypes = new HashSet<string>(StringComparer.Ordinal);

        if (jsonContextTypeName is not null)
        {
            foreach (var handler in handlers)
            {
                if (!importedTypeNames.Contains(handler.MessageTypeName)
                    && jsonContextTypeNames.Contains(handler.MessageTypeName))
                {
                    messageTypes.Add(handler.MessageTypeName);
                }

                if (handler.ResponseTypeName is not null
                    && !importedTypeNames.Contains(handler.ResponseTypeName)
                    && jsonContextTypeNames.Contains(handler.ResponseTypeName))
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
        }

        var sortedMessageTypeNames = messageTypes
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        var sortedSagaTypeNames = sagas
            .Select(s => s.SagaTypeName)
            .Distinct(StringComparer.Ordinal)
            .Where(t => !importedSagaTypeNames.Contains(t))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        var sortedHandlerTypeNames = handlers
            .Select(h => h.HandlerTypeName)
            .Distinct(StringComparer.Ordinal)
            .Where(t => !importedHandlerTypeNames.Contains(t))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        using var builder = new MessagingDependencyInjectionFileBuilder(moduleName, assemblyName);

        builder.WriteHeader();
        builder.WriteBeginNamespace();
        builder.WriteBeginClass();
        builder.WriteBeginRegistrationMethod(sortedMessageTypeNames, sortedSagaTypeNames, sortedHandlerTypeNames);

        // When JsonContext is specified, emit AOT registrations at the top of the method.
        if (jsonContextTypeName is not null)
        {
            builder.WriteSectionComment("AOT Configuration");
            builder.WriteStrictModeConfiguration();
            builder.WriteJsonTypeInfoResolverRegistration(jsonContextTypeName);

            // Framework base types are emitted as typeof(...) so MessageType.Complete
            // can resolve their URN through the runtime naming convention.
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

                // Walk the hierarchy; include registered user types and framework base types.
                if (hierarchyLookup.TryGetValue(messageTypeName, out var hierarchy))
                {
                    foreach (var typeName in hierarchy)
                    {
                        if (enclosed.Contains(typeName))
                        {
                            continue;
                        }

                        if (messageTypes.Contains(typeName) || IsFrameworkBaseType(typeName))
                        {
                            enclosed.Add(typeName);
                        }
                    }
                }

                // Precompute specificity scores; List.Sort reorders during comparison,
                // so scoring inside the comparator produces non-deterministic results.
                var scores = new Dictionary<string, int>(enclosed.Count, StringComparer.Ordinal);

                foreach (var typeName in enclosed)
                {
                    if (!hierarchyLookup.TryGetValue(typeName, out var h))
                    {
                        scores[typeName] = 0;
                        continue;
                    }

                    var score = 0;
                    foreach (var other in enclosed)
                    {
                        if (other != typeName && h.Contains(other))
                        {
                            score++;
                        }
                    }
                    scores[typeName] = score;
                }

                enclosed.Sort((a, b) => scores[b].CompareTo(scores[a]));

                enclosedTypesMap[messageTypeName] = enclosed;
            }

            builder.WriteSectionComment("Message Type Serializers");

            foreach (var messageType in messageTypes.OrderBy(t => t, StringComparer.Ordinal))
            {
                enclosedTypesMap.TryGetValue(messageType, out var enclosedTypes);
                builder.WriteMessageConfiguration(
                    messageType,
                    jsonContextTypeName,
                    enclosedTypes);
            }
        }

        if (sagas.Count > 0)
        {
            builder.WriteSectionComment("Saga Configuration");

            foreach (var saga in sagas)
            {
                builder.WriteSagaConfiguration(saga.SagaTypeName, saga.StateTypeName, jsonContextTypeName);
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

        builder.WriteEndRegistrationMethod();
        builder.WriteEndClass();
        builder.WriteEndNamespace();

        addSource(builder.HintName + ".g.cs", builder.ToSourceText());
    }

    /// <summary>
    /// Determines whether the fully qualified display string refers to a framework base type
    /// (<c>Mocha.IEventRequest</c> or closed <c>Mocha.IEventRequest&lt;T&gt;</c>) that must
    /// flow into <c>EnclosedTypes</c> without being registered as a standalone message type.
    /// </summary>
    private static bool IsFrameworkBaseType(string fullyQualifiedName)
    {
        if (fullyQualifiedName == SyntaxConstants.IEventRequestDisplay)
        {
            return true;
        }

        return fullyQualifiedName.StartsWith(SyntaxConstants.IEventRequestOfTDisplayPrefix, StringComparison.Ordinal)
            && fullyQualifiedName.EndsWith(">", StringComparison.Ordinal);
    }
}
