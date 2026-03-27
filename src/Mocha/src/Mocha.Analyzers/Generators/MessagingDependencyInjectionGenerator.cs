using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Mocha.Analyzers.FileBuilders;

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

        if (handlers.Count == 0 && sagas.Count == 0)
        {
            return;
        }

        using var builder = new MessagingDependencyInjectionFileBuilder(moduleName, assemblyName);

        builder.WriteHeader();
        builder.WriteBeginNamespace();
        builder.WriteBeginClass();
        builder.WriteBeginRegistrationMethod();

        // --- Batch Handlers ---
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

        // --- Consumers ---
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

        // --- Request Handlers ---
        var requestHandlers = handlers
            .Where(h => h.Kind == MessagingHandlerKind.RequestResponse || h.Kind == MessagingHandlerKind.Send)
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

        // --- Event Handlers ---
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

        // --- Sagas ---
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
