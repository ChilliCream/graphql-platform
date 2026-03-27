using System.Security.Cryptography;
using System.Text;
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
        HintName = _extensionsClassName + "." + ComputeSalt(assemblyName);
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
        var methodName = handler.Kind switch
        {
            MessagingHandlerKind.Batch => "AddBatchHandler",
            MessagingHandlerKind.Consumer => "AddConsumer",
            MessagingHandlerKind.RequestResponse => "AddRequestHandler",
            MessagingHandlerKind.Send => "AddRequestHandler",
            MessagingHandlerKind.Event => "AddEventHandler",
            _ => throw new ArgumentOutOfRangeException()
        };

        Writer.WriteIndentedLine(
            "global::Mocha.MessageBusHostBuilderExtensions.{0}<", methodName);
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("{0}>(builder);", handler.HandlerTypeName);
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
