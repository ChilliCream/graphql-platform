using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis.Text;
using static HotChocolate.Types.Analyzers.WellKnownTypes;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class RequestMiddlewareFileBuilder : IDisposable
{
    private readonly string _moduleName;
    private readonly string _ns;
    private readonly string _id;
    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _disposed;

    public RequestMiddlewareFileBuilder(string moduleName, string ns)
    {
        _moduleName = moduleName;
        _ns = ns;

        _id = Guid.NewGuid().ToString("N");
        _sb = PooledObjects.GetStringBuilder();
        _writer = new(_sb);
    }

    public void WriteHeader()
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
        _writer.WriteLine();
        _writer.Write(Properties.SourceGenResources.InterceptsAttribute);
    }

    public string WriteBeginClass()
    {
        var typeName = $"{_moduleName}MiddlewareFactories{_id}";
        _writer.WriteIndentedLine("public static class {0}", typeName);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
        return typeName;
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteFactory(int middlewareIndex, RequestMiddlewareInfo middleware)
    {
        _writer.WriteIndentedLine("// {0}", middleware.TypeName);
        _writer.WriteIndentedLine(
            "private static global::{0} CreateMiddleware{1}()",
            RequestCoreMiddleware,
            middlewareIndex);

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("=> (core, next) =>");

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("{");

                using (_writer.IncreaseIndent())
                {
                    WriteCtorServiceResolution(middleware.CtorParameters);
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
                _writer.WriteIndentedLine("};");
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
                        "var cp{0} = core.Services.GetRequiredService<{1}>();",
                        i,
                        parameter.TypeName);
                    break;

                case RequestMiddlewareParameterKind.Service when parameter.IsNullable:
                    _writer.WriteIndentedLine(
                        "var cp{0} = core.Services.GetService<{1}>();",
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
        _writer.WriteIndented("var middleware = new {0}(", typeName);

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            if(i > 0)
            {
                _writer.Write(", ");
            }

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
                        "var ip{0} = context.Services.GetRequiredService<{1}>();",
                        i,
                        parameter.TypeName);
                    break;

                case RequestMiddlewareParameterKind.Service when parameter.IsNullable:
                    _writer.WriteIndentedLine(
                        "var ip{0} = context.Services.GetService<{1}>();",
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
        _writer.WriteIndented("await middleware.{0}(", methodName);

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            if(i > 0)
            {
                _writer.Write(", ");
            }

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

    public void WriteInterceptMethod(
        int middlewareIndex,
        (string FilePath, int LineNumber, int CharacterNumber) location)
    {
        _writer.WriteLine();

        _writer.WriteIndentedLine(
            "[InterceptsLocation(\"{0}\", {1}, {2})]",
            location.FilePath.Replace("\\", "\\\\"),
            location.LineNumber,
            location.CharacterNumber);
        _writer.WriteIndentedLine(
            "public static global::{0} UseRequestGen{1}<TMiddleware>(",
            RequestExecutorBuilder,
            middlewareIndex);

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("this {0} builder) where TMiddleware : class", RequestExecutorBuilder);
        }

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine(
                "=> builder.UseRequest(CreateMiddleware{2}());",
                _moduleName,
                "MiddlewareFactories",
                middlewareIndex);
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
