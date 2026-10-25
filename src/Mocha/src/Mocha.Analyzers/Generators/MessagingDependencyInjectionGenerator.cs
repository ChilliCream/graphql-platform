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
        var handlers = DeduplicationHelper
            .SelectRepresentatives(
                syntaxInfos.OfType<MessagingHandlerInfo>().Where(h => h.Diagnostics.Count == 0),
                h => h.HandlerTypeName,
                h => h.XmlDocumentation,
                h => h.Location)
            .OrderBy(h => h.OrderByKey)
            .ToList();

        var sagas = DeduplicationHelper
            .SelectRepresentatives(
                syntaxInfos.OfType<SagaInfo>().Where(s => s.Diagnostics.Count == 0),
                s => s.SagaTypeName,
                s => s.XmlDocumentation,
                s => s.Location)
            .OrderBy(s => s.OrderByKey)
            .ToList();

        var contextOnlyTypes = syntaxInfos.OfType<ContextOnlyMessageInfo>().OrderBy(c => c.OrderByKey).ToList();

        if (handlers.Count == 0 && sagas.Count == 0 && contextOnlyTypes.Count == 0)
        {
            return;
        }

        // Find the module info to check for JsonContext.
        string? jsonContextTypeName = null;
        var isAotPublish = false;
        SourceMetadataOptionsInfo? sourceMetadataOptions = null;

        foreach (var info in syntaxInfos)
        {
            if (jsonContextTypeName is null && info is MessagingModuleInfo moduleInfo)
            {
                jsonContextTypeName = moduleInfo.JsonContextTypeName;
            }

            if (info is AotPublishInfo aotPublishInfo)
            {
                isAotPublish = aotPublishInfo.IsAotPublish;
            }

            if (info is SourceMetadataOptionsInfo optionsInfo)
            {
                sourceMetadataOptions = optionsInfo;
            }
        }

        sourceMetadataOptions ??= SourceMetadataOptionsInfo.Default;

        // Collect type names imported from module methods invoked in this compilation.
        // Those module methods have already registered their own message, saga, and handler metadata.
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

        // Collect per-type declaration metadata (doc + span) captured in the declaration pipeline,
        // keyed by fully qualified type name for the message/response join in GetMessages.
        var declaredTypesByName = new Dictionary<string, DeclaredTypeInfo>(StringComparer.Ordinal);

        // Message types the user registers explicitly via AddMessage<T>(). The generated module emits only the
        // descriptor callback for these; the user's own call performs the AddMessage registration.
        var explicitAddMessageTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in syntaxInfos)
        {
            if (info is MessageDeclarationsInfo messageDeclarations)
            {
                foreach (var declared in messageDeclarations.Declarations)
                {
                    if (!declaredTypesByName.ContainsKey(declared.TypeName))
                    {
                        declaredTypesByName.Add(declared.TypeName, declared);
                    }
                }

                foreach (var typeName in messageDeclarations.ExplicitAddMessageTypeNames)
                {
                    explicitAddMessageTypeNames.Add(typeName);
                }
            }
        }

        var messages = GetMessages(handlers, contextOnlyTypes, importedTypeNames, declaredTypesByName);
        var messagesByTypeName = messages.ToDictionary(m => m.MessageTypeName, StringComparer.Ordinal);

        // Collect all unique message types that this module registers with
        // generated serializer configuration. Only types in the local JsonSerializerContext
        // and not already registered by invoked modules qualify.
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

        var sortedMessageTypeNames = messageTypes.OrderBy(t => t, StringComparer.Ordinal).ToList();

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

        // Message types discovered from handlers and dispatch call sites that are declared in this compilation
        // but are not covered by the local JsonSerializerContext. They get AddMessage plus a Source-only
        // descriptor callback, without a serializer or enclosed types. Saga state types are excluded (they are
        // registered through the saga path, not as message types) as are types already covered by the context
        // path or by an invoked module.
        var sagaStateTypeNames = new HashSet<string>(
            sagas.Select(s => s.StateTypeName),
            StringComparer.Ordinal);

        var metadataOnlyMessageTypeNames = declaredTypesByName.Keys
            .Where(t => !messageTypes.Contains(t)
                && !importedTypeNames.Contains(t)
                && !sagaStateTypeNames.Contains(t))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        using var builder = new MessagingDependencyInjectionFileBuilder(
            moduleName,
            assemblyName,
            sourceMetadataOptions);
        var messageInitializers = new List<MessageInitializer>();
        var sagaInitializers = new List<SagaInitializer>();
        var consumerInitializers = new List<ConsumerInitializer>();

        builder.WriteHeader();
        builder.WriteBeginNamespace();
        builder.WriteBeginClass();
        builder.WriteBeginRegistrationMethod(sortedMessageTypeNames, sortedSagaTypeNames, sortedHandlerTypeNames);

        // When JsonContext is specified, emit AOT registrations at the top of the method.
        if (jsonContextTypeName is not null)
        {
            builder.WriteSectionComment("AOT Configuration");
            if (isAotPublish)
            {
                builder.WriteStrictModeConfiguration();
            }

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
                messagesByTypeName.TryGetValue(messageType, out var messageMetadata);
                var methodName = builder.CreateGeneratedMethodName(messageType, "MessageType");
                messageInitializers.Add(
                    new MessageInitializer(
                        methodName,
                        messageType,
                        jsonContextTypeName,
                        enclosedTypes,
                        messageMetadata?.XmlDocumentation,
                        messageMetadata?.Location));

                builder.WriteMessageConfiguration(messageType, methodName);
            }
        }

        if (metadataOnlyMessageTypeNames.Count > 0)
        {
            builder.WriteSectionComment("Message Types");

            foreach (var messageType in metadataOnlyMessageTypeNames)
            {
                // Types the user already registers with an explicit AddMessage<T>() get only the descriptor
                // callback; every other declared message type also gets a generated AddMessage.
                if (!explicitAddMessageTypeNames.Contains(messageType))
                {
                    builder.WriteAddMessage(messageType);
                }

                // Every metadata-only type carries a declaration span, so a Source-only descriptor callback is
                // emitted whenever SourceMetadata emission is enabled.
                if (sourceMetadataOptions.Emit && declaredTypesByName.TryGetValue(messageType, out var declared))
                {
                    var methodName = builder.CreateGeneratedMethodName(messageType, "MessageType");
                    messageInitializers.Add(
                        new MessageInitializer(
                            methodName,
                            messageType,
                            null,
                            null,
                            declared.XmlDocumentation,
                            declared.Location));

                    builder.WriteMessageDescriptor(messageType, methodName);
                }
            }
        }

        if (sagas.Count > 0)
        {
            builder.WriteSectionComment("Saga Configuration");

            foreach (var saga in sagas)
            {
                var sagaJsonContextTypeName = jsonContextTypeNames.Contains(saga.StateTypeName)
                    ? jsonContextTypeName
                    : null;
                var methodName = builder.CreateGeneratedMethodName(saga.SagaTypeName, "Saga");
                sagaInitializers.Add(new SagaInitializer(methodName, saga, sagaJsonContextTypeName));

                builder.WriteSagaConfiguration(saga.SagaTypeName, saga.StateTypeName, methodName);
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
                WriteConsumerConfiguration(builder, handler, consumerInitializers);
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
                WriteConsumerConfiguration(builder, handler, consumerInitializers);
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
                WriteConsumerConfiguration(builder, handler, consumerInitializers);
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
                WriteConsumerConfiguration(builder, handler, consumerInitializers);
            }
        }

        builder.WriteEndRegistrationMethod();

        foreach (var initializer in messageInitializers)
        {
            builder.WriteMessageInitializer(
                initializer.MethodName,
                initializer.MessageTypeName,
                initializer.JsonContextTypeName,
                initializer.EnclosedTypes,
                initializer.XmlDocumentation,
                initializer.Location);
        }

        foreach (var initializer in sagaInitializers)
        {
            builder.WriteSagaInitializer(
                initializer.MethodName,
                initializer.Saga.StateTypeName,
                initializer.JsonContextTypeName,
                initializer.Saga.XmlDocumentation,
                initializer.Saga.Location);
        }

        foreach (var initializer in consumerInitializers)
        {
            builder.WriteConsumerInitializer(
                initializer.MethodName,
                initializer.Handler.XmlDocumentation,
                initializer.Handler.DeclarationLocation);
        }

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

    private static List<MessageMetadata> GetMessages(
        List<MessagingHandlerInfo> handlers,
        List<ContextOnlyMessageInfo> contextOnlyTypes,
        HashSet<string> importedTypeNames,
        Dictionary<string, DeclaredTypeInfo> declaredTypesByName)
    {
        var messages = new Dictionary<string, MessageMetadata>();

        foreach (var handler in handlers)
        {
            Add(handler.MessageTypeName, handler.MessageNamespace, fallbackLocation: null);

            if (handler.ResponseTypeName is not null)
            {
                Add(handler.ResponseTypeName, handler.ResponseNamespace ?? string.Empty, fallbackLocation: null);
            }
        }

        foreach (var contextOnly in contextOnlyTypes)
        {
            Add(contextOnly.MessageTypeName, contextOnly.MessageNamespace, contextOnly.Location);
        }

        return messages.Values.OrderBy(m => m.MessageTypeName).ToList();

        void Add(string messageTypeName, string messageNamespace, LocationInfo? fallbackLocation)
        {
            if (importedTypeNames.Contains(messageTypeName))
            {
                return;
            }

            // The type's own declaration (captured in the declaration pipeline) always wins.
            // The fallback location is only used for types without a source declaration in this
            // compilation (for example the [JsonSerializable] attribute span of a cross-assembly type).
            string? xmlDocumentation = null;
            LocationInfo? location = null;

            if (declaredTypesByName.TryGetValue(messageTypeName, out var declared))
            {
                xmlDocumentation = declared.XmlDocumentation;
                location = declared.Location;
            }

            location ??= fallbackLocation;

            if (messages.TryGetValue(messageTypeName, out var existing))
            {
                messages[messageTypeName] = existing with
                {
                    XmlDocumentation = existing.XmlDocumentation ?? xmlDocumentation,
                    Location = existing.Location ?? location
                };
                return;
            }

            messages.Add(
                messageTypeName,
                new MessageMetadata(messageTypeName, messageNamespace, xmlDocumentation, location));
        }
    }

    private sealed record MessageMetadata(
        string MessageTypeName,
        string MessageNamespace,
        string? XmlDocumentation,
        LocationInfo? Location);

    private static void WriteConsumerConfiguration(
        MessagingDependencyInjectionFileBuilder moduleBuilder,
        MessagingHandlerInfo handler,
        List<ConsumerInitializer> initializers)
    {
        var methodName = moduleBuilder.CreateGeneratedMethodName(handler.HandlerTypeName, "Consumer");
        initializers.Add(new ConsumerInitializer(methodName, handler));
        moduleBuilder.WriteConsumerConfiguration(handler, methodName);
    }

    private sealed record MessageInitializer(
        string MethodName,
        string MessageTypeName,
        string? JsonContextTypeName,
        IReadOnlyList<string>? EnclosedTypes,
        string? XmlDocumentation,
        LocationInfo? Location);

    private sealed record SagaInitializer(string MethodName, SagaInfo Saga, string? JsonContextTypeName);

    private sealed record ConsumerInitializer(string MethodName, MessagingHandlerInfo Handler);
}
