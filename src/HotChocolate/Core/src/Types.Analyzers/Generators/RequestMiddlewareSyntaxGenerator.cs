using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis.Text;
using static HotChocolate.Types.Analyzers.WellKnownTypes;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class RequestMiddlewareSyntaxGenerator
{
    private readonly StringBuilder _sb;
    private readonly CodeWriter _writer;

    public RequestMiddlewareSyntaxGenerator(StringBuilder sb)
    {
        _sb = sb;
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
        _writer.WriteIndentedLine("public static class {0}", typeName);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteFactory(RequestMiddlewareInfo middleware)
    {
        _writer.WriteIndentedLine("public static global::{0} {1}()", RequestCoreMiddleware, middleware.Name);

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("=> (core, next) =>");

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("{");

                using (_writer.IncreaseIndent())
                {
                    WriteCtorServiceResolution(middleware.CtorParameters);
                    _writer.WriteLine();
                    WriteFactory(middleware.TypeName, middleware.CtorParameters);
                    _writer.WriteIndentedLine("return async context =>");
                    _writer.WriteIndentedLine("{");

                    using (_writer.IncreaseIndent())
                    {
                        WriteInvokeServiceResolution(middleware.InvokeParameters);
                        WriteInvoke(middleware.InvokeMethodName, middleware.InvokeParameters);
                    }
                    _writer.WriteIndentedLine("};");
                }
                _writer.WriteIndentedLine("}");
            }
        }
    }

    private void WriteCtorServiceResolution(List<RequestMiddlewareParameterInfo> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            switch (parameter.Kind)
            {
                case RequestMiddlewareParameterKind.Service when !parameter.IsNullable:
                    _writer.WriteIndentedLine(
                        "var cp{0} = core.Services.GetRequiredService<global::{1}>();",
                        i,
                        parameter.TypeName);
                    break;

                case RequestMiddlewareParameterKind.Service when parameter.IsNullable:
                    _writer.WriteIndentedLine(
                        "var cp{0} = core.Services.GetService<global::{1}>();",
                        i,
                        parameter.TypeName);
                    break;

                case RequestMiddlewareParameterKind.SchemaService when !parameter.IsNullable:
                    _writer.WriteIndentedLine(
                        "var cp{0} = core.SchemaServices.GetRequiredService<global::{1}>();",
                        i,
                        parameter.TypeName);
                    break;

                case RequestMiddlewareParameterKind.SchemaService when parameter.IsNullable:
                    _writer.WriteIndentedLine(
                        "var cp{0} = core.SchemaServices.GetService<global::{1}>();",
                        i,
                        parameter.TypeName);
                    break;

                case RequestMiddlewareParameterKind.Schema:
                    _writer.WriteIndentedLine(
                        "var cp{0} = core.SchemaServices.GetRequiredService<global::{1}>();",
                        i,
                        Schema);
                    break;

                case RequestMiddlewareParameterKind.Next:
                    break;

                default:
                    throw new NotSupportedException("Service kind not supported in location.");
            }
        }
    }

    private void WriteFactory(string typeName, List<RequestMiddlewareParameterInfo> parameters)
    {
        _writer.WriteIndented("var middleware = new global::{0}(", typeName);

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            if (parameter.Kind is RequestMiddlewareParameterKind.Next)
            {
                _writer.Write("next");
            }
            else
            {
                _writer.Write("cp{0}", i);
            }
        }

        _writer.WriteLine(");");
    }

    private void WriteInvokeServiceResolution(List<RequestMiddlewareParameterInfo> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            switch (parameter.Kind)
            {
                case RequestMiddlewareParameterKind.Service when !parameter.IsNullable:
                    _writer.WriteIndentedLine(
                        "var ip{0} = context.Services.GetRequiredService<global::{1}>();",
                        i,
                        parameter.TypeName);
                    break;

                case RequestMiddlewareParameterKind.Service when parameter.IsNullable:
                    _writer.WriteIndentedLine(
                        "var ip{0} = context.Services.GetService<global::{1}>();",
                        i,
                        parameter.TypeName);
                    break;

                case RequestMiddlewareParameterKind.Schema:
                    _writer.WriteIndentedLine(
                        "var ip{0} = context.Schema;",
                        i,
                        Schema);
                    break;

                case RequestMiddlewareParameterKind.Next:
                    break;

                case RequestMiddlewareParameterKind.Context:
                    break;

                default:
                    throw new NotSupportedException("Service kind not supported in location.");
            }
        }
    }

    private void WriteInvoke(string methodName, List<RequestMiddlewareParameterInfo> parameters)
    {
        _writer.WriteIndented("await middleware.InvokeAsync(");

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            if (parameter.Kind is RequestMiddlewareParameterKind.Next)
            {
                _writer.Write("next");
            }
            else if (parameter.Kind is RequestMiddlewareParameterKind.Context)
            {
                _writer.Write("context");
            }
            else
            {
                _writer.Write("ip{0}", i);
            }
        }

        _writer.WriteLine(").ConfigureAwait(false);");
    }

    public override string ToString()
        => _sb.ToString();

    public SourceText ToSourceText()
        => SourceText.From(ToString(), Encoding.UTF8);
}
