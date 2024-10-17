using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ModuleFileBuilder : IDisposable
{
    private readonly string _moduleName;
    private readonly string _ns;
    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _disposed;

    public ModuleFileBuilder(string moduleName, string ns)
    {
        _moduleName = moduleName;
        _ns = ns;
        _sb = PooledObjects.GetStringBuilder();
        _writer = new CodeWriter(_sb);
    }

    public void WriteHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteLine();
    }

    public void WriteBeginNamespace()
    {
        _writer.WriteIndentedLine("namespace {0}", _ns);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndNamespace()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteBeginClass()
    {
        _writer.WriteIndentedLine("public static partial class {0}RequestExecutorBuilderExtensions", _moduleName);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteBeginRegistrationMethod()
    {
        _writer.WriteIndentedLine(
            "public static IRequestExecutorBuilder Add{0}(this IRequestExecutorBuilder builder)",
            _moduleName);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndRegistrationMethod()
    {
        _writer.WriteIndentedLine("return builder;");
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteRegisterType(string typeName)
        => _writer.WriteIndentedLine("builder.AddType<global::{0}>();", typeName);

    public void WriteRegisterTypeExtension(string typeName, bool staticType)
        => _writer.WriteIndentedLine(
            staticType
                ? "builder.AddTypeExtension(typeof(global::{0}));"
                : "builder.AddTypeExtension<global::{0}>();",
            typeName);

    public void WriteRegisterObjectTypeExtension(string runtimeTypeName, string extensionType)
    {
        _writer.WriteIndentedLine(
            "AddObjectTypeExtension_8734371<{0}>(builder, {1}.Initialize);",
            runtimeTypeName,
            extensionType);
    }

    public void WriteRegisterInterfaceTypeExtension(string runtimeTypeName, string extensionType)
    {
        _writer.WriteIndentedLine(
            "AddInterfaceTypeExtension_8734371<{0}>(builder, {1}.Initialize);",
            runtimeTypeName,
            extensionType);
    }

    public void WriteRegisterObjectTypeExtensionHelpers()
    {
        _writer.WriteLine();
        _writer.WriteIndentedLine("private static void AddObjectTypeExtension_8734371<T>(");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("global::HotChocolate.Execution.Configuration.IRequestExecutorBuilder builder,");
            _writer.WriteIndentedLine("Action<IObjectTypeDescriptor<T>> initialize)");
        }

        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("builder.ConfigureSchema(sb =>");
            _writer.WriteIndentedLine("{");

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("string typeName = typeof(T).FullName!;");
                _writer.WriteIndentedLine("string typeKey = $\"8734371_Type_ObjectType<{typeName}>\";");
                _writer.WriteIndentedLine("string hooksKey = $\"8734371_Hooks_ObjectType<{typeName}>\";");
                _writer.WriteLine();
                _writer.WriteIndentedLine("if (!sb.ContextData.ContainsKey(typeKey))");
                _writer.WriteIndentedLine("{");

                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine("sb.AddObjectType<T>(");
                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine("descriptor =>");
                        _writer.WriteIndentedLine("{");

                        using (_writer.IncreaseIndent())
                        {
                            _writer.WriteIndentedLine(
                                "var hooks = (global::System.Collections.Generic.List<"
                                + "Action<IObjectTypeDescriptor<T>>>)"
                                + "descriptor.Extend().Context.ContextData[hooksKey]!;");
                            _writer.WriteIndentedLine("foreach (var configure in hooks)");
                            _writer.WriteIndentedLine("{");

                            using (_writer.IncreaseIndent())
                            {
                                _writer.WriteIndentedLine("configure(descriptor);");
                            }

                            _writer.WriteIndentedLine("};");
                        }

                        _writer.WriteIndentedLine("});");
                    }

                    _writer.WriteIndentedLine("sb.ContextData.Add(typeKey, null);");
                }

                _writer.WriteIndentedLine("}");
                _writer.WriteLine();

                _writer.WriteIndentedLine("if (!sb.ContextData.TryGetValue(hooksKey, out var value))");
                _writer.WriteIndentedLine("{");

                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine(
                        "value = new System.Collections.Generic.List<Action<IObjectTypeDescriptor<T>>>();");
                    _writer.WriteIndentedLine("sb.ContextData.Add(hooksKey, value);");
                }

                _writer.WriteIndentedLine("}");
                _writer.WriteLine();
                _writer.WriteIndentedLine(
                    "((System.Collections.Generic.List<Action<IObjectTypeDescriptor<T>>>)value!)"
                    + ".Add(initialize);");
            }

            _writer.WriteIndentedLine("});");
        }

        _writer.WriteIndentedLine("}");
    }

    public void WriteRegisterInterfaceTypeExtensionHelpers()
    {
        _writer.WriteLine();
        _writer.WriteIndentedLine("private static void AddInterfaceTypeExtension_8734371<T>(");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("global::HotChocolate.Execution.Configuration.IRequestExecutorBuilder builder,");
            _writer.WriteIndentedLine("Action<IInterfaceTypeDescriptor<T>> initialize)");
        }

        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("builder.ConfigureSchema(sb =>");
            _writer.WriteIndentedLine("{");

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("string typeName = typeof(T).FullName!;");
                _writer.WriteIndentedLine("string typeKey = $\"8734371_Type_InterfaceType<{typeName}>\";");
                _writer.WriteIndentedLine("string hooksKey = $\"8734371_Hooks_InterfaceType<{typeName}>\";");
                _writer.WriteLine();
                _writer.WriteIndentedLine("if (!sb.ContextData.ContainsKey(typeKey))");
                _writer.WriteIndentedLine("{");

                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine("sb.AddInterfaceType<T>(");
                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine("descriptor =>");
                        _writer.WriteIndentedLine("{");

                        using (_writer.IncreaseIndent())
                        {
                            _writer.WriteIndentedLine(
                                "var hooks = (global::System.Collections.Generic.List<"
                                + "Action<IInterfaceTypeDescriptor<T>>>)"
                                + "descriptor.Extend().Context.ContextData[hooksKey]!;");
                            _writer.WriteIndentedLine("foreach (var configure in hooks)");
                            _writer.WriteIndentedLine("{");

                            using (_writer.IncreaseIndent())
                            {
                                _writer.WriteIndentedLine("configure(descriptor);");
                            }

                            _writer.WriteIndentedLine("};");
                        }

                        _writer.WriteIndentedLine("});");
                    }

                    _writer.WriteIndentedLine("sb.ContextData.Add(typeKey, null);");
                }

                _writer.WriteIndentedLine("}");
                _writer.WriteLine();

                _writer.WriteIndentedLine("if (!sb.ContextData.TryGetValue(hooksKey, out var value))");
                _writer.WriteIndentedLine("{");

                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine(
                        "value = new System.Collections.Generic.List<Action<IInterfaceTypeDescriptor<T>>>();");
                    _writer.WriteIndentedLine("sb.ContextData.Add(hooksKey, value);");
                }

                _writer.WriteIndentedLine("}");
                _writer.WriteLine();
                _writer.WriteIndentedLine(
                    "((System.Collections.Generic.List<Action<IInterfaceTypeDescriptor<T>>>)value!)"
                    + ".Add(initialize);");
            }

            _writer.WriteIndentedLine("});");
        }

        _writer.WriteIndentedLine("}");
    }

    public void WriteRegisterDataLoader(string typeName)
        => _writer.WriteIndentedLine("builder.AddDataLoader<global::{0}>();", typeName);

    public void WriteRegisterDataLoader(
        string typeName,
        string interfaceTypeName,
        bool withInterface)
    {
        if (withInterface)
        {
            _writer.WriteIndentedLine(
                "builder.AddDataLoader<global::{0}, global::{1}>();",
                interfaceTypeName,
                typeName);
        }
        else
        {
            _writer.WriteIndentedLine(
                "builder.AddDataLoader<global::{0}>();",
                typeName);
        }
    }

    public void WriteRegisterDataLoaderGroup(string typeName, string interfaceTypeName)
        => _writer.WriteIndentedLine(
            "builder.Services.AddScoped<global::{0}, global::{1}>();",
            interfaceTypeName,
            typeName);

    public void WriteTryAddOperationType(OperationType type)
    {
        _writer.WriteIndentedLine("builder.ConfigureSchema(");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("b => b.TryAddRootType(");

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("() => new global::HotChocolate.Types.ObjectType(");

                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine("d => d.Name(global::HotChocolate.Types.OperationTypeNames.{0})),", type);
                }

                _writer.WriteIndentedLine("HotChocolate.Language.OperationType.{0}));", type);
            }
        }
    }

    public override string ToString()
        => _sb.ToString();

    public SourceText ToSourceText()
        => SourceText.From(ToString(), Encoding.UTF8);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        PooledObjects.Return(_sb);
        _sb = default!;
        _writer = default!;
        _disposed = true;
    }
}
