using System.Security.Cryptography;
using System.Text;
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
        HintName = _extensionsClassName + "." + ComputeSalt(assemblyName);
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

    public void WriteBeginRegistrationMethod()
    {
        Writer.WriteIndentedLine("public static global::Mocha.Mediator.IMediatorHostBuilder {0}(", _methodName);
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("this global::Mocha.Mediator.IMediatorHostBuilder builder)");
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();

        Writer.WriteIndentedLine("var services = builder.Services;");
        Writer.WriteIndentedLine("var lifetime = builder.Options.ServiceLifetime;");
    }

    public void WriteHandlerRegistration(HandlerInfo handler)
    {
        switch (handler.Kind)
        {
            case HandlerKind.CommandVoid:
                WriteServiceDescriptor(
                    "global::Mocha.Mediator.ICommandHandler<{0}>",
                    handler.HandlerTypeName,
                    handler.MessageTypeName);
                break;
            case HandlerKind.CommandResponse:
                WriteServiceDescriptor(
                    "global::Mocha.Mediator.ICommandHandler<{0}, {1}>",
                    handler.HandlerTypeName,
                    handler.MessageTypeName,
                    handler.ResponseTypeName!);
                break;
            case HandlerKind.Query:
                WriteServiceDescriptor(
                    "global::Mocha.Mediator.IQueryHandler<{0}, {1}>",
                    handler.HandlerTypeName,
                    handler.MessageTypeName,
                    handler.ResponseTypeName!);
                break;
        }
    }

    public void WriteNotificationHandlerRegistration(NotificationHandlerInfo handler)
    {
        // Use TryAddEnumerable to prevent duplicate handler registrations
        // when the generated Add{Module} extension is called more than once
        // (e.g. in tests or modular startup).
        Writer.WriteIndentedLine(
            "global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddEnumerable(services, new global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(global::Mocha.Mediator.INotificationHandler<{0}>), typeof({1}), lifetime));",
            handler.NotificationTypeName,
            handler.HandlerTypeName);
    }

    /// <summary>
    /// Writes the opening of a ConfigureMediator lambda for deferred pipeline registrations.
    /// </summary>
    public void WriteBeginConfigureMediator()
    {
        Writer.WriteIndentedLine("global::Mocha.Mediator.MediatorHostBuilderExtensions.ConfigureMediator(builder, static b =>");
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    /// <summary>
    /// Writes the closing of a ConfigureMediator lambda.
    /// </summary>
    public void WriteEndConfigureMediator()
    {
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("});");
    }

    /// <summary>
    /// Writes a pipeline registration for a handler (inside ConfigureMediator lambda, using 'b').
    /// </summary>
    public void WritePipelineRegistration(HandlerInfo handler)
    {
        var (terminalMethod, responseType) = handler.Kind switch
        {
            HandlerKind.CommandVoid => ($"BuildVoidCommandTerminal<{handler.MessageTypeName}>()", null),
            HandlerKind.CommandResponse => ($"BuildCommandTerminal<{handler.MessageTypeName}, {handler.ResponseTypeName}>()", handler.ResponseTypeName),
            HandlerKind.Query => ($"BuildQueryTerminal<{handler.MessageTypeName}, {handler.ResponseTypeName}>()", handler.ResponseTypeName),
            _ => throw new ArgumentOutOfRangeException()
        };

        WritePipelineConfiguration(handler.MessageTypeName, responseType, terminalMethod);
    }

    /// <summary>
    /// Writes a pipeline registration for a notification group (inside ConfigureMediator lambda, using 'b').
    /// </summary>
    public void WriteNotificationPipelineRegistration(string notificationType,
        List<NotificationHandlerInfo> groupHandlers)
    {
        var handlerTypeArgs = string.Join(", ",
            groupHandlers.Select(h => $"typeof({h.HandlerTypeName})"));

        var terminalMethod = $"BuildNotificationTerminal<{notificationType}>(new global::System.Type[] {{ {handlerTypeArgs} }})";
        WritePipelineConfiguration(notificationType, null, terminalMethod);
    }

    private void WritePipelineConfiguration(string messageTypeName, string? responseTypeName, string terminalMethod)
    {
        Writer.WriteIndentedLine("b.RegisterPipeline(new global::Mocha.Mediator.MediatorPipelineConfiguration");
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("MessageType = typeof({0}),", messageTypeName);

        if (responseTypeName is not null)
        {
            Writer.WriteIndentedLine("ResponseType = typeof({0}),", responseTypeName);
        }

        Writer.WriteIndentedLine("Terminal = global::Mocha.Mediator.PipelineBuilder.{0}", terminalMethod);
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("});");
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

    private void WriteServiceDescriptor(string serviceTypeFormat, string implementationType, params object[] typeArgs)
    {
        // Use TryAdd to prevent duplicate handler registrations
        // when the generated Add{Module} extension is called more than once.
        var serviceType = string.Format(serviceTypeFormat, typeArgs);
        Writer.WriteIndentedLine(
            "global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAdd(services, new global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof({0}), typeof({1}), lifetime));",
            serviceType,
            implementationType);
    }

#pragma warning disable CA5351 // MD5 is used for non-security hashing (file name salting)
    private static readonly MD5 s_md5 = MD5.Create();
#pragma warning restore CA5351

    private static string ComputeSalt(string assemblyName)
    {
        byte[] hashBytes;

        lock (s_md5)
        {
            hashBytes = s_md5.ComputeHash(Encoding.UTF8.GetBytes(assemblyName));
        }

        var base64 = Convert.ToBase64String(hashBytes, Base64FormattingOptions.None);

        return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
