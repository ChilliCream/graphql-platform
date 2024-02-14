using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class OperationFieldSyntaxGenerator: IDisposable
{
    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _disposed;

    public OperationFieldSyntaxGenerator()
    {
        _sb = StringBuilderPool.Get();
        _writer = new CodeWriter(_sb);
    }

    public void WriterHeader()
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

    public void WriteConfigureMethod(IEnumerable<OperationInfo> operations)
    {
        _writer.WriteIndentedLine("protected override void Configure(");
        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("global::HotChocolate.Types.IObjectTypeDescriptor descriptor)");
        }
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();

        var typeIndex = 0;
        
        foreach (var group in operations.GroupBy(t => t.TypeName))
        {
            var typeName = $"type{++typeIndex}";
            _writer.WriteIndentedLine("Type {0} = typeof({1});", typeName, group.Key);

            foreach (var operation in group)
            {
                _writer.WriteIndentedLine(
                    "descriptor.Field({0}.GetMember(\"{1}\", System.Reflection.BindingFlags.Public)[0]);",
                    typeName,
                    operation.MethodName);
            }
            
            _writer.WriteLine();
        }
        
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
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