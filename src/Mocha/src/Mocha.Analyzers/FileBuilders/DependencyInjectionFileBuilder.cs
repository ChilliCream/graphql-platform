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

    public DependencyInjectionFileBuilder(string moduleName, string assemblyName) : base(moduleName)
    {
        _extensionsClassName = moduleName + "MediatorBuilderExtensions";
        _methodName = "Add" + moduleName;
        HintName = _extensionsClassName + "." + HashHelper.ComputeSalt(assemblyName);
    }

    public string HintName { get; }

    /// <inheritdoc />
    protected override string Namespace => "Microsoft.Extensions.DependencyInjection";

    /// <inheritdoc />
    public override void WriteBeginClass()
    {
        Writer.WriteGeneratedAttribute();
        Writer.WriteIndentedLine("public static class {0}", _extensionsClassName);
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    /// <summary>
    /// Writes the opening of the registration extension method, including the
    /// <c>[MediatorModuleInfo]</c> attribute with the message types array.
    /// </summary>
    /// <param name="messageTypeNames">
    /// The fully qualified message type names to include in the attribute, or <see langword="null"/> to omit the attribute.
    /// </param>
    public void WriteBeginRegistrationMethod(IReadOnlyList<string>? messageTypeNames = null)
    {
        if (messageTypeNames is { Count: > 0 })
        {
            Writer.WriteIndentedLine(
                "[global::Mocha.Mediator.MediatorModuleInfo(MessageTypes = new global::System.Type[]");
            Writer.WriteIndentedLine("{");
            Writer.IncreaseIndent();
            foreach (var typeName in messageTypeNames)
            {
                Writer.WriteIndentedLine("typeof({0}),", typeName);
            }
            Writer.DecreaseIndent();
            Writer.WriteIndentedLine("})]");
        }

        Writer.WriteIndentedLine("public static global::Mocha.Mediator.IMediatorHostBuilder {0}(", _methodName);
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("this global::Mocha.Mediator.IMediatorHostBuilder builder)");
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    /// <summary>
    /// Writes an AddHandlerConfiguration call for a command/query handler.
    /// </summary>
    public void WriteHandlerConfiguration(HandlerInfo handler)
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

        WriteAddHandlerConfiguration(
            handler.HandlerTypeName,
            handler.MessageTypeName,
            handler.ResponseTypeName,
            kindEnum,
            $"global::Mocha.Mediator.PipelineBuilder.{terminalCall}");
    }

    /// <summary>
    /// Writes an AddHandlerConfiguration call for a notification handler.
    /// </summary>
    public void WriteNotificationHandlerConfiguration(string notificationType, NotificationHandlerInfo handler)
    {
        WriteAddHandlerConfiguration(
            handler.HandlerTypeName,
            notificationType,
            responseTypeName: null,
            "Notification",
            $"global::Mocha.Mediator.PipelineBuilder.BuildNotificationPipeline<{handler.HandlerTypeName}, {notificationType}>()");
    }

    private void WriteAddHandlerConfiguration(
        string handlerTypeName,
        string messageTypeName,
        string? responseTypeName,
        string kindEnum,
        string terminalCall)
    {
        Writer.WriteIndentedLine(
            "global::Mocha.Mediator.MediatorHostBuilderHandlerExtensions.AddHandlerConfiguration<{0}>(builder,",
            handlerTypeName);
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("new global::Mocha.Mediator.MediatorHandlerConfiguration");
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("HandlerType = typeof({0}),", handlerTypeName);
        Writer.WriteIndentedLine("MessageType = typeof({0}),", messageTypeName);

        if (responseTypeName is not null)
        {
            Writer.WriteIndentedLine("ResponseType = typeof({0}),", responseTypeName);
        }

        Writer.WriteIndentedLine("Kind = global::Mocha.Mediator.MediatorHandlerKind.{0},", kindEnum);
        Writer.WriteIndentedLine("Delegate = {0}", terminalCall);
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("});");
        Writer.DecreaseIndent();
    }

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
}
