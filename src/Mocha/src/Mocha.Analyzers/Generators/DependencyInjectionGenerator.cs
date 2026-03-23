using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Mocha.Analyzers.FileBuilders;

namespace Mocha.Analyzers.Generators;

/// <summary>
/// Provides a source generator that emits the <c>AddHandlers</c> extension method on
/// <c>IMediatorHostBuilder</c>, registering handlers and pipelines into the dependency injection container.
/// </summary>
public sealed class DependencyInjectionGenerator : ISyntaxGenerator
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
            .OfType<HandlerInfo>()
            .Where(h => h.Diagnostics.Count == 0)
            .OrderBy(h => h.OrderByKey)
            .ToList();

        var notificationHandlers = syntaxInfos
            .OfType<NotificationHandlerInfo>()
            .Where(h => h.Diagnostics.Count == 0)
            .OrderBy(h => h.OrderByKey)
            .ToList();

        if (handlers.Count == 0 && notificationHandlers.Count == 0)
        {
            return;
        }

        using var builder = new DependencyInjectionFileBuilder(moduleName, assemblyName);

        builder.WriteHeader();
        builder.WriteBeginNamespace();
        builder.WriteBeginClass();
        builder.WriteBeginRegistrationMethod();

        // Register handlers
        if (handlers.Count > 0)
        {
            builder.WriteSectionComment("Register handlers");

            foreach (var handler in handlers)
            {
                builder.WriteHandlerRegistration(handler);
            }
        }

        // Register notification handlers
        if (notificationHandlers.Count > 0)
        {
            builder.WriteSectionComment("Register notification handlers");

            foreach (var handler in notificationHandlers)
            {
                builder.WriteNotificationHandlerRegistration(handler);
            }
        }

        // Register pipelines (all handlers + notifications) via deferred ConfigureMediator
        var notificationGroups = notificationHandlers
            .GroupBy(h => h.NotificationTypeName)
            .OrderBy(g => g.Key)
            .ToList();

        if (handlers.Count > 0 || notificationGroups.Count > 0)
        {
            builder.WriteSectionComment("Register pipelines");
            builder.WriteBeginConfigureMediator();

            foreach (var handler in handlers)
            {
                builder.WritePipelineRegistration(handler);
            }

            foreach (var group in notificationGroups)
            {
                builder.WriteNotificationPipelineRegistration(
                    group.Key,
                    group.OrderBy(h => h.HandlerTypeName).ToList());
            }

            builder.WriteEndConfigureMediator();
        }

        builder.WriteEndRegistrationMethod();
        builder.WriteEndClass();
        builder.WriteEndNamespace();

        addSource(builder.HintName + ".g.cs", builder.ToSourceText());
    }
}
