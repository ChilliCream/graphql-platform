using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class OperationFieldFileBuilder : IDisposable
{
    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _first = true;
    private bool _disposed;

    public OperationFieldFileBuilder()
    {
        _sb = PooledObjects.GetStringBuilder();
        _writer = new CodeWriter(_sb);
    }

    public void WriteHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteLine();
    }

    public void WriteBeginNamespace(string ns)
    {
        _writer.WriteIndentedLine("namespace {0}", ns);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndNamespace()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
        _writer.WriteLine();
    }

    public void WriteBeginClass(string typeName)
    {
        if (!_first)
        {
            _writer.WriteLine();
        }
        _first = false;

        _writer.WriteIndentedLine("public sealed class {0}", typeName);

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine(": global::HotChocolate.Types.ObjectTypeExtension");
        }
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteConfigureMethod(OperationType type, IEnumerable<OperationInfo> operations)
    {
        _writer.WriteIndentedLine("protected override void Configure(");
        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("global::HotChocolate.Types.IObjectTypeDescriptor descriptor)");
        }
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();

        _writer.WriteIndentedLine("var bindingFlags = System.Reflection.BindingFlags.Public |");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("System.Reflection.BindingFlags.NonPublic |");
            _writer.WriteIndentedLine("System.Reflection.BindingFlags.Static;");
        }

        _writer.WriteIndentedLine("descriptor.Name({0});", GetOperationConstant(type));

        var typeIndex = 0;
        foreach (var group in operations.GroupBy(t => t.TypeName))
        {
            _writer.WriteLine();

            var typeName = $"type{++typeIndex}";
            _writer.WriteIndentedLine("var {0} = typeof({1});", typeName, group.Key);

            foreach (var operation in group)
            {
                _writer.WriteIndentedLine(
                    "descriptor.Field({0}.GetMember(\"{1}\", bindingFlags)[0]);",
                    typeName,
                    operation.MethodName);
            }
        }

        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    private static string GetOperationConstant(OperationType type)
        => type switch
        {
            OperationType.Query => "global::HotChocolate.Types.OperationTypeNames.Query",
            OperationType.Mutation => "global::HotChocolate.Types.OperationTypeNames.Mutation",
            OperationType.Subscription => "global::HotChocolate.Types.OperationTypeNames.Subscription",
            _ => throw new InvalidOperationException(),
        };

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
