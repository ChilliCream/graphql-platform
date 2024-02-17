using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using static HotChocolate.Types.Analyzers.WellKnownTypes;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class RegisterMiddlewareSyntaxGenerator
{
    private readonly string _moduleName;
    private readonly string _ns;
    private readonly StringBuilder _sb;
    private readonly CodeWriter _writer;
    private string _g = Guid.NewGuid().ToString("N");

    public RegisterMiddlewareSyntaxGenerator(StringBuilder sb, string moduleName, string ns)
    {
        _moduleName = moduleName;
        _ns = ns;
        _sb = sb;
        _writer = new CodeWriter(_sb);
    }

    public void WriterHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteIndentedLine("using Microsoft.Extensions.DependencyInjection;");
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
        _writer.WriteIndentedLine("public static partial class {0}_{1}_MiddlewareFactories", _moduleName, _g);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }
    
    public void WriteMiddlewareExtensionMethod(
        string methodName,
        (string FilePath, int LineNumber, int CharacterNumber) location)
    {
        _writer.WriteLine();
        
        _writer.WriteIndentedLine(
            "[InterceptsLocation(\"{0}\", {1}, {2})]",
            location.FilePath,
            location.LineNumber,
            location.CharacterNumber);
        _writer.WriteIndentedLine("public static global::{0} Use{1}<TMiddleware>(", RequestExecutorBuilder, methodName);

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("this {0} builder) where TMiddleware : class", RequestExecutorBuilder);
        }

        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine(
                "builder.UseRequest({0}{1}.{2});",
                _moduleName,
                "MiddlewareFactories",
                methodName);
        }
        _writer.WriteIndentedLine("}");
    }
}