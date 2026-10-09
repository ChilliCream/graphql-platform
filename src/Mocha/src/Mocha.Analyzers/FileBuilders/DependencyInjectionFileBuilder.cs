using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.FileBuilders;

/// <summary>
/// Builds the module-prefixed mediator builder extensions source file that registers
/// handlers and pipelines into the dependency injection container.
/// </summary>
public sealed class DependencyInjectionFileBuilder : FileBuilderBase
{
    private readonly string _extensionsClassName;
    private readonly string _methodName;
    private readonly SourceMetadataOptionsInfo _sourceMetadataOptions;

    public DependencyInjectionFileBuilder(
        string moduleName,
        string assemblyName,
        SourceMetadataOptionsInfo sourceMetadataOptions) : base(moduleName, assemblyName)
    {
        _extensionsClassName = moduleName + "MediatorBuilderExtensions";
        _methodName = "Add" + moduleName;
        _sourceMetadataOptions = sourceMetadataOptions;
        HintName = _extensionsClassName + "." + HashHelper.ComputeSalt(assemblyName);
    }

    public string HintName { get; }

    /// <inheritdoc />
    protected override string Namespace => "Microsoft.Extensions.DependencyInjection";

    /// <inheritdoc />
    public override void WriteBeginClass()
    {
        Writer.WriteGeneratedAttribute();
        Writer.WriteIndentedLine("public static partial class {0}", _extensionsClassName);
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    /// <summary>
    /// Writes the opening of the registration extension method, including the
    /// <c>[MediatorModuleInfo]</c> attribute with the message and handler types arrays.
    /// </summary>
    /// <param name="messageTypeNames">
    /// The fully qualified message type names to include in the attribute, or <see langword="null"/> to omit.
    /// </param>
    /// <param name="handlerTypeNames">
    /// The fully qualified handler type names to include in the attribute, or <see langword="null"/> to omit.
    /// </param>
    public void WriteBeginRegistrationMethod(
        IReadOnlyList<string>? messageTypeNames = null,
        IReadOnlyList<string>? handlerTypeNames = null)
    {
        var hasMessages = messageTypeNames is { Count: > 0 };
        var hasHandlers = handlerTypeNames is { Count: > 0 };

        if (hasMessages || hasHandlers)
        {
            // Build the list of property assignments to emit, so we can handle
            // comma placement correctly (no trailing comma on the last property).
            var properties = new List<(string Name, IReadOnlyList<string> Types)>();

            if (hasMessages)
            {
                properties.Add(("MessageTypes", messageTypeNames!));
            }

            if (hasHandlers)
            {
                properties.Add(("HandlerTypes", handlerTypeNames!));
            }

            Writer.WriteIndentedLine("[global::Mocha.Mediator.MediatorModuleInfo(");
            Writer.IncreaseIndent();

            for (var i = 0; i < properties.Count; i++)
            {
                var (name, types) = properties[i];
                var isLast = i == properties.Count - 1;

                Writer.WriteIndentedLine("{0} = new global::System.Type[]", name);
                Writer.WriteIndentedLine("{");
                Writer.IncreaseIndent();
                foreach (var typeName in types)
                {
                    Writer.WriteIndentedLine("typeof({0}),", typeName);
                }
                Writer.DecreaseIndent();
                Writer.WriteIndentedLine(isLast ? "}" : "},");
            }

            Writer.DecreaseIndent();
            Writer.WriteIndentedLine(")]");
        }

        Writer.WriteIndentedLine("public static global::Mocha.Mediator.IMediatorHostBuilder {0}(", _methodName);
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("this global::Mocha.Mediator.IMediatorHostBuilder builder)");
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    /// <summary>
    /// Writes a mediator handler registration that points at a generated initializer method.
    /// </summary>
    public void WriteHandlerRegistration(string handlerTypeName, string initializerMethodName)
    {
        Writer.WriteIndentedLine("global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAdd(");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("builder.Services,");
        Writer.WriteIndentedLine("new global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor(");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("typeof({0}),", handlerTypeName);
        Writer.WriteIndentedLine("typeof({0}),", handlerTypeName);
        Writer.WriteIndentedLine("builder.Options.ServiceLifetime));");
        Writer.DecreaseIndent();
        Writer.DecreaseIndent();

        Writer.WriteIndentedLine("global::Mocha.Mediator.MediatorHostBuilderExtensions.ConfigureMediator(");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("builder,");
        Writer.WriteIndentedLine(
            "static b => b.AddHandler<{0}>({1}));",
            handlerTypeName,
            initializerMethodName);
        Writer.DecreaseIndent();
    }

    public void WriteHandlerInitializer(string methodName, HandlerInfo handler)
    {
        var (kindEnum, terminalCall) = handler.Kind switch
        {
            HandlerKind.Command => (
                "Command",
                $"BuildCommandPipeline<{handler.HandlerTypeName}, {handler.MessageTypeName}>()"),
            HandlerKind.CommandResponse => (
                "CommandResponse",
                $"BuildCommandResponsePipeline<{handler.HandlerTypeName}, {handler.MessageTypeName}, {handler.ResponseTypeName}>()"),
            HandlerKind.Query => (
                "Query",
                $"BuildQueryPipeline<{handler.HandlerTypeName}, {handler.MessageTypeName}, {handler.ResponseTypeName}>()"),
            _ => throw new ArgumentOutOfRangeException()
        };

        WriteHandlerInitializer(
            methodName,
            handler.MessageTypeName,
            handler.ResponseTypeName,
            kindEnum,
            $"global::Mocha.Mediator.PipelineBuilder.{terminalCall}",
            handler.XmlDocumentation,
            handler.Location);
    }

    public void WriteNotificationHandlerInitializer(string methodName, NotificationHandlerInfo handler)
        => WriteHandlerInitializer(
            methodName,
            handler.NotificationTypeName,
            responseTypeName: null,
            "Notification",
            $"global::Mocha.Mediator.PipelineBuilder.BuildNotificationPipeline<{handler.HandlerTypeName}, {handler.NotificationTypeName}>()",
            handler.XmlDocumentation,
            handler.Location);

    public void WriteSectionComment(string comment)
    {
        Writer.WriteLine();
        Writer.WriteIndentedLine("// {0}", comment);
    }

    public void WriteEndRegistrationMethod()
    {
        Writer.WriteLine();
        Writer.WriteIndentedLine("return builder;");
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("}");
    }

    private void WriteHandlerInitializer(
        string methodName,
        string messageTypeName,
        string? responseTypeName,
        string kindEnum,
        string terminalCall,
        string? xmlDocumentation,
        LocationInfo? location)
    {
        Writer.WriteLine();
        Writer.WriteIndentedLine(
            "private static void {0}(global::Mocha.Mediator.IMediatorHandlerDescriptor descriptor)",
            methodName);
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("var configuration = descriptor.Extend().Configuration;");
        Writer.WriteIndentedLine("configuration.MessageType = typeof({0});", messageTypeName);

        if (responseTypeName is not null)
        {
            Writer.WriteIndentedLine("configuration.ResponseType = typeof({0});", responseTypeName);
        }

        Writer.WriteIndentedLine("configuration.Kind = global::Mocha.Mediator.MediatorHandlerKind.{0};", kindEnum);
        Writer.WriteIndentedLine("configuration.Delegate = {0};", terminalCall);

        Writer.WriteSourceMetadataAssignment(
            "configuration.Source",
            "global::Mocha.SourceMetadata",
            "global::Mocha.DeclarationLocation",
            AssemblyName,
            xmlDocumentation,
            location,
            _sourceMetadataOptions);

        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("}");
    }
}
