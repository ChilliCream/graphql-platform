using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Mocha.Analyzers.FileBuilders;
using Mocha.Analyzers.Utils;

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
        var handlers = DeduplicationHelper
            .SelectRepresentatives(
                syntaxInfos.OfType<HandlerInfo>().Where(h => h.Diagnostics.Count == 0),
                h => h.HandlerTypeName,
                h => h.XmlDocumentation,
                h => h.Location)
            .OrderBy(h => h.OrderByKey)
            .ToList();

        var notificationHandlers = DeduplicationHelper
            .SelectRepresentatives(
                syntaxInfos.OfType<NotificationHandlerInfo>().Where(h => h.Diagnostics.Count == 0),
                h => h.HandlerTypeName,
                h => h.XmlDocumentation,
                h => h.Location)
            .OrderBy(h => h.OrderByKey)
            .ToList();

        if (handlers.Count == 0 && notificationHandlers.Count == 0)
        {
            return;
        }

        // Collect all unique message types for the [MediatorModuleInfo] attribute.
        var allMessageTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var handler in handlers)
        {
            allMessageTypeNames.Add(handler.MessageTypeName);
        }

        foreach (var handler in notificationHandlers)
        {
            allMessageTypeNames.Add(handler.NotificationTypeName);
        }

        var sortedMessageTypeNames = allMessageTypeNames.OrderBy(t => t, StringComparer.Ordinal).ToList();

        var sortedHandlerTypeNames = handlers
            .Select(h => h.HandlerTypeName)
            .Concat(notificationHandlers.Select(h => h.HandlerTypeName))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        var sourceMetadataOptions =
            syntaxInfos.OfType<SourceMetadataOptionsInfo>().FirstOrDefault() ?? SourceMetadataOptionsInfo.Default;

        using var builder = new DependencyInjectionFileBuilder(moduleName, assemblyName, sourceMetadataOptions);

        var notificationGroups = notificationHandlers.GroupBy(h => h.NotificationTypeName).OrderBy(g => g.Key).ToList();

        builder.WriteHeader();
        builder.WriteBeginNamespace();
        builder.WriteBeginClass();
        builder.WriteBeginRegistrationMethod(sortedMessageTypeNames, sortedHandlerTypeNames);

        if (handlers.Count > 0 || notificationGroups.Count > 0)
        {
            builder.WriteSectionComment("Register handlers");

            foreach (var handler in handlers)
            {
                var methodName = builder.CreateGeneratedMethodName(handler.HandlerTypeName, "Handler");
                builder.WriteHandlerRegistration(handler.HandlerTypeName, methodName);
            }

            foreach (var group in notificationGroups)
            {
                foreach (var handler in group.OrderBy(h => h.HandlerTypeName))
                {
                    var methodName = builder.CreateGeneratedMethodName(handler.HandlerTypeName, "Handler");
                    builder.WriteHandlerRegistration(handler.HandlerTypeName, methodName);
                }
            }
        }

        builder.WriteEndRegistrationMethod();

        foreach (var handler in handlers)
        {
            var methodName = builder.CreateGeneratedMethodName(handler.HandlerTypeName, "Handler");
            builder.WriteHandlerInitializer(methodName, handler);
        }

        foreach (var group in notificationGroups)
        {
            foreach (var handler in group.OrderBy(h => h.HandlerTypeName))
            {
                var methodName = builder.CreateGeneratedMethodName(handler.HandlerTypeName, "Handler");
                builder.WriteNotificationHandlerInitializer(methodName, handler);
            }
        }

        builder.WriteEndClass();
        builder.WriteEndNamespace();

        addSource(builder.HintName + ".g.cs", builder.ToSourceText());
    }
}
