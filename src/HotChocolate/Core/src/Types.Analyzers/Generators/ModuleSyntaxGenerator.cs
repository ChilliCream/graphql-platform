using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class ModuleSyntaxGenerator : IDisposable
{
    private readonly string _moduleName;
    private readonly string _ns;
    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _disposed;

    public ModuleSyntaxGenerator(string moduleName, string ns)
    {
        _moduleName = moduleName;
        _ns = ns;
        _sb = StringBuilderPool.Get();
        _writer = new CodeWriter(_sb);
    }

    public void WriterHeader()
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

    public void WriteRegisterDataLoader(string typeName)
        => _writer.WriteIndentedLine("builder.AddDataLoader<global::{0}>();", typeName);

    public void WriteRegisterDataLoader(string typeName, string interfaceTypeName)
        => _writer.WriteIndentedLine("builder.AddDataLoader<global::{0}, global::{1}>();", interfaceTypeName, typeName);

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

        StringBuilderPool.Return(_sb);
        _sb = default!;
        _writer = default!;
        _disposed = true;
    }
}