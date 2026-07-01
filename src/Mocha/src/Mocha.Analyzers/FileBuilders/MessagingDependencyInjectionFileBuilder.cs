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
    /// Writes the opening of the registration extension method, including the
    /// <c>[MessagingModuleInfo]</c> attribute with the message, saga, and handler types arrays.
    /// </summary>
    /// <param name="messageTypeNames">
    /// The fully qualified message type names to include in the attribute, or <see langword="null"/> to omit.
    /// </param>
    /// <param name="sagaTypeNames">
    /// The fully qualified saga type names to include in the attribute, or <see langword="null"/> to omit.
    /// </param>
    /// <param name="handlerTypeNames">
    /// The fully qualified handler type names to include in the attribute, or <see langword="null"/> to omit.
    /// </param>
    public void WriteBeginRegistrationMethod(
        IReadOnlyList<string>? messageTypeNames = null,
        IReadOnlyList<string>? sagaTypeNames = null,
        IReadOnlyList<string>? handlerTypeNames = null)
    {
        var hasMessages = messageTypeNames is { Count: > 0 };
        var hasSagas = sagaTypeNames is { Count: > 0 };
        var hasHandlers = handlerTypeNames is { Count: > 0 };

        if (hasMessages || hasSagas || hasHandlers)
        {
            // Build the list of property assignments to emit, so we can handle
            // comma placement correctly (no trailing comma on the last property).
            var properties = new List<(string Name, IReadOnlyList<string> Types)>();

            if (hasMessages)
            {
                properties.Add(("MessageTypes", messageTypeNames!));
            }

            if (hasSagas)
            {
                properties.Add(("SagaTypes", sagaTypeNames!));
            }

            if (hasHandlers)
            {
                properties.Add(("HandlerTypes", handlerTypeNames!));
            }

            Writer.WriteIndentedLine("[global::Mocha.MessagingModuleInfo(");
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

        Writer.WriteIndentedLine("public static global::Mocha.IMessageBusHostBuilder {0}(", _methodName);
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("this global::Mocha.IMessageBusHostBuilder builder)");
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    /// <summary>
    /// Writes the strict mode configuration that requires explicit message type registration.
    /// </summary>
    public void WriteStrictModeConfiguration()
    {
        Writer.WriteIndentedLine("global::Mocha.MessageBusHostBuilderExtensions.ModifyOptions(");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("builder,");
        Writer.WriteIndentedLine("static o => o.IsAotCompatible = true);");
        Writer.DecreaseIndent();
    }

    /// <summary>
    /// Writes the registration of a <c>JsonSerializerContext</c> as a type info resolver.
    /// </summary>
    /// <param name="jsonContextTypeName">The fully qualified type name of the JsonSerializerContext.</param>
    public void WriteJsonTypeInfoResolverRegistration(string jsonContextTypeName)
    {
        Writer.WriteIndentedLine("global::Mocha.MessageBusHostBuilderExtensions.AddJsonTypeInfoResolver(");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("builder,");
        Writer.WriteIndentedLine("{0}.Default);", jsonContextTypeName);
        Writer.DecreaseIndent();
    }

    /// <summary>
    /// Writes a message configuration registration with a pre-built JSON serializer.
    /// </summary>
    /// <param name="messageTypeName">The fully qualified message type name.</param>
    /// <param name="jsonContextTypeName">The fully qualified type name of the JsonSerializerContext.</param>
    /// <param name="enclosedTypes">
    /// The pre-computed enclosed types sorted by specificity (including framework base types
    /// such as <c>IEventRequest</c> and closed <c>IEventRequest&lt;T&gt;</c>), or
    /// <see langword="null"/> to omit.
    /// </param>
    public void WriteMessageConfiguration(
        string messageTypeName,
        string jsonContextTypeName,
        IReadOnlyList<string>? enclosedTypes = null)
    {
        Writer.WriteIndentedLine("global::Mocha.MessageBusHostBuilderExtensions.AddMessageConfiguration(");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("builder,");
        Writer.WriteIndentedLine("new global::Mocha.MessagingMessageConfiguration");
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("MessageType = typeof({0}),", messageTypeName);
        Writer.WriteIndentedLine("Serializer = new global::Mocha.JsonMessageSerializer(");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("{0}.Default.GetTypeInfo(typeof({1}))!),", jsonContextTypeName, messageTypeName);
        Writer.DecreaseIndent();

        if (enclosedTypes is { Count: > 0 })
        {
            Writer.WriteIndentedLine(
                "EnclosedTypes = global::System.Collections.Immutable.ImmutableArray.Create<global::System.Type>(");
            Writer.IncreaseIndent();
            for (var i = 0; i < enclosedTypes.Count; i++)
            {
                var typeName = enclosedTypes[i];
                var isLast = i == enclosedTypes.Count - 1;
                Writer.WriteIndentedLine(isLast ? "typeof({0})" : "typeof({0}),", typeName);
            }
            Writer.DecreaseIndent();
            Writer.WriteIndentedLine("),");
        }

        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("});");
        Writer.DecreaseIndent();
    }

    /// <summary>
    /// Writes a saga configuration registration. When a <paramref name="jsonContextTypeName"/>
    /// is provided, includes a pre-built JSON state serializer; otherwise emits a configuration
    /// with only the saga type.
    /// </summary>
    /// <param name="sagaTypeName">The fully qualified saga type name.</param>
    /// <param name="stateTypeName">The fully qualified saga state type name.</param>
    /// <param name="jsonContextTypeName">
    /// The fully qualified type name of the JsonSerializerContext, or <see langword="null"/>
    /// to omit the state serializer.
    /// </param>
    public void WriteSagaConfiguration(string sagaTypeName, string stateTypeName, string? jsonContextTypeName)
    {
        Writer.WriteIndentedLine("global::Mocha.MessageBusHostBuilderExtensions.AddSagaConfiguration<");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("{0}>(", sagaTypeName);
        Writer.DecreaseIndent();
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("builder,");
        Writer.WriteIndentedLine("new global::Mocha.MessagingSagaConfiguration");
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
        Writer.WriteIndentedLine("SagaType = typeof({0}),", sagaTypeName);

        if (jsonContextTypeName is not null)
        {
            Writer.WriteIndentedLine("StateSerializer = new global::Mocha.Sagas.JsonSagaStateSerializer(");
            Writer.IncreaseIndent();
            Writer.WriteIndentedLine("{0}.Default.GetTypeInfo(typeof({1}))!),", jsonContextTypeName, stateTypeName);
            Writer.DecreaseIndent();
        }

        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("});");
        Writer.DecreaseIndent();
    }

    /// <summary>
    /// Writes a handler registration call for the specified messaging handler.
    /// </summary>
    /// <param name="handler">The messaging handler info to register.</param>
    public void WriteHandlerRegistration(MessagingHandlerInfo handler)
    {
        var factoryCall = handler.Kind switch
        {
            MessagingHandlerKind.Event => $"Subscribe<{handler.HandlerTypeName}, {handler.MessageTypeName}>()",
            MessagingHandlerKind.Send => $"Send<{handler.HandlerTypeName}, {handler.MessageTypeName}>()",
            MessagingHandlerKind.RequestResponse =>
                $"Request<{handler.HandlerTypeName}, {handler.MessageTypeName}, {handler.ResponseTypeName}>()",
            MessagingHandlerKind.Consumer => $"Consume<{handler.HandlerTypeName}, {handler.MessageTypeName}>()",
            MessagingHandlerKind.Batch => $"Batch<{handler.HandlerTypeName}, {handler.MessageTypeName}>()",
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
