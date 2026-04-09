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

        var notificationGroups = notificationHandlers
            .GroupBy(h => h.NotificationTypeName)
            .OrderBy(g => g.Key)
            .ToList();

        builder.WriteHeader();
        builder.WriteBeginNamespace();
        builder.WriteBeginClass();
        builder.WriteBeginRegistrationMethod();

        // Register all handler configurations
        if (handlers.Count > 0 || notificationGroups.Count > 0)
        {
            builder.WriteSectionComment("Register handler configurations");

            foreach (var handler in handlers)
            {
                builder.WriteHandlerConfiguration(handler);
            }

            foreach (var group in notificationGroups)
            {
                foreach (var handler in group.OrderBy(h => h.HandlerTypeName))
                {
                    builder.WriteNotificationHandlerConfiguration(group.Key, handler);
                }
            }
        }

        builder.WriteEndRegistrationMethod();
        builder.WriteEndClass();
        builder.WriteEndNamespace();

        addSource(builder.HintName + ".g.cs", builder.ToSourceText());
    }
}
