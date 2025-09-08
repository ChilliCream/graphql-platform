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

    public void WriteEnsureObjectTypeExtensionIsRegistered(string runtimeTypeName)
        => _writer.WriteIndentedLine("builder.AddType<ObjectType<{0}>>();", runtimeTypeName);

    public void WriteEnsureInterfaceTypeExtensionIsRegistered(string runtimeTypeName)
        => _writer.WriteIndentedLine("builder.AddType<InterfaceType<{0}>>();", runtimeTypeName);

    public void WriteRegisterTypeExtension(string key, string runtimeTypeName, string extensionType)
    {
        _writer.WriteIndentedLine(
            "builder.ConfigureDescriptorContext(ctx => ctx.TypeConfiguration.TryAdd<{0}>(",
            runtimeTypeName);
        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("\"{0}\",", key);
            _writer.WriteIndentedLine("() => {0}.Initialize));", extensionType);
        }
    }

    public void WriteRegisterRootTypeExtension(string key, OperationType operation, string extensionType)
    {
        _writer.WriteIndentedLine("builder.ConfigureDescriptorContext(ctx => ctx.TypeConfiguration.TryAdd(");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("\"{0}\",", key);
            _writer.WriteIndentedLine("global::HotChocolate.Types.OperationTypeNames.{0},", operation);
            _writer.WriteIndentedLine("() => {0}.Initialize));", extensionType);
        }
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
        _sb = null!;
        _writer = null!;
        _disposed = true;
    }
}
