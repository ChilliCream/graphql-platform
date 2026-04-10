using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.FileBuilders;

/// <summary>
/// Builds the module-prefixed message bus builder extensions source file that registers
/// messaging handlers and sagas into the dependency injection container.
/// </summary>
public sealed class MessagingDependencyInjectionFileBuilder : FileBuilderBase
{
    private readonly string _extensionsClassName;
    private readonly string _methodName;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagingDependencyInjectionFileBuilder"/> class.
    /// </summary>
    /// <param name="moduleName">The module name used to prefix generated type names.</param>
    /// <param name="assemblyName">The assembly name used to compute a unique file hint name.</param>
    public MessagingDependencyInjectionFileBuilder(string moduleName, string assemblyName) : base(moduleName)
    {
        _extensionsClassName = moduleName + "MessageBusBuilderExtensions";
        _methodName = "Add" + moduleName;
        HintName = _extensionsClassName + "." + HashHelper.ComputeSalt(assemblyName);
    }

    /// <summary>
    /// Gets the hint name used to uniquely identify the generated source file.
    /// </summary>
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
    /// Writes the opening of the registration extension method.
    /// </summary>
    public void WriteBeginRegistrationMethod()
    {
        Writer.WriteIndentedLine("public static global::Mocha.IMessageBusHostBuilder {0}(", _methodName);
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("this global::Mocha.IMessageBusHostBuilder builder)");
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    /// <summary>
    /// Writes a handler registration call for the specified messaging handler.
    /// </summary>
    /// <param name="handler">The messaging handler info to register.</param>
    public void WriteHandlerRegistration(MessagingHandlerInfo handler)
    {
        var factoryCall = handler.Kind switch
        {
            MessagingHandlerKind.Event =>
                $"Subscribe<{handler.HandlerTypeName}, {handler.MessageTypeName}>()",
            MessagingHandlerKind.Send =>
                $"Send<{handler.HandlerTypeName}, {handler.MessageTypeName}>()",
            MessagingHandlerKind.RequestResponse =>
                $"Request<{handler.HandlerTypeName}, {handler.MessageTypeName}, {handler.ResponseTypeName}>()",
            MessagingHandlerKind.Consumer =>
                $"Consume<{handler.HandlerTypeName}, {handler.MessageTypeName}>()",
            MessagingHandlerKind.Batch =>
                $"Batch<{handler.HandlerTypeName}, {handler.MessageTypeName}>()",
            _ => throw new ArgumentOutOfRangeException()
        };

        Writer.WriteIndentedLine(
            "global::Mocha.MessageBusHostBuilderExtensions.AddHandlerConfiguration<{0}>(builder,",
            handler.HandlerTypeName);
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("new global::Mocha.MessagingHandlerConfiguration");
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("HandlerType = typeof({0}),", handler.HandlerTypeName);
        Writer.WriteIndentedLine("Factory = global::Mocha.ConsumerFactory.{0}", factoryCall);
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("});");
        Writer.DecreaseIndent();
    }

    /// <summary>
    /// Writes a saga registration call for the specified saga.
    /// </summary>
    /// <param name="saga">The saga info to register.</param>
    public void WriteSagaRegistration(SagaInfo saga)
    {
        Writer.WriteIndentedLine(
            "global::Mocha.MessageBusHostBuilderExtensions.AddSaga<");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("{0}>(builder);", saga.SagaTypeName);
        Writer.DecreaseIndent();
    }

    /// <summary>
    /// Writes a section comment to visually separate groups of registrations.
    /// </summary>
    /// <param name="comment">The section label to write.</param>
    public void WriteSectionComment(string comment)
    {
        Writer.WriteLine();
        Writer.WriteIndentedLine("// --- {0} ---", comment);
    }

    /// <summary>
    /// Writes the closing of the registration extension method, including the return statement.
    /// </summary>
    public void WriteEndRegistrationMethod()
    {
        Writer.WriteLine();
        Writer.WriteIndentedLine("return builder;");
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("}");
    }
}
