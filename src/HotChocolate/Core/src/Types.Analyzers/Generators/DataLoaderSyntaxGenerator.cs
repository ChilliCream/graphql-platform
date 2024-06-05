using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class DataLoaderSyntaxGenerator : IDisposable
{
    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _disposed;

    public DataLoaderSyntaxGenerator()
    {
        _sb = StringBuilderPool.Get();
        _writer = new CodeWriter(_sb);
    }

    public void WriteHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteIndentedLine("using Microsoft.Extensions.DependencyInjection;");
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

    public void WriteDataLoaderInterface(
        string name,
        bool isPublic,
        DataLoaderKind kind,
        ITypeSymbol key,
        ITypeSymbol value)
    {
        _writer.WriteIndentedLine(
            "{0} interface {1}",
            isPublic
                ? "public"
                : "internal",
            name);
        _writer.IncreaseIndent();

        _writer.WriteIndentedLine(
            kind is DataLoaderKind.Group
                ? ": global::GreenDonut.IDataLoader<{0}, {1}[]>"
                : ": global::GreenDonut.IDataLoader<{0}, {1}>",
            key.ToFullyQualified(),
            value.ToFullyQualified());

        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("{");
        _writer.WriteIndentedLine("}");
        _writer.WriteLine();
    }

    public void WriteBeginDataLoaderClass(
        string name,
        string interfaceName,
        bool isPublic,
        DataLoaderKind kind,
        ITypeSymbol key,
        ITypeSymbol value)
    {
        _writer.WriteIndentedLine(
            "{0} sealed class {1}",
            isPublic
                ? "public"
                : "internal",
            name);
        _writer.IncreaseIndent();

        switch (kind)
        {
            case DataLoaderKind.Batch:
                _writer.WriteIndentedLine(
                    ": global::GreenDonut.BatchDataLoader<{0}, {1}>",
                    key.ToFullyQualified(),
                    value.ToFullyQualified());
                break;

            case DataLoaderKind.Group:
                _writer.WriteIndentedLine(
                    ": global::GreenDonut.GroupedDataLoader<{0}, {1}>",
                    key.ToFullyQualified(),
                    value.ToFullyQualified());
                break;

            case DataLoaderKind.Cache:
                _writer.WriteIndentedLine(
                    ": global::GreenDonut.CacheDataLoader<{0}, {1}>",
                    key.ToFullyQualified(),
                    value.ToFullyQualified());
                break;
        }

        _writer.WriteIndentedLine(", {0}", interfaceName);
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndDataLoaderClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
        _writer.WriteLine();
    }

    public void WriteDataLoaderConstructor(
        string name,
        DataLoaderKind kind)
    {
        _writer.WriteIndentedLine("private readonly global::System.IServiceProvider _services;");
        _writer.WriteLine();

        if (kind is DataLoaderKind.Batch or DataLoaderKind.Group)
        {
            _writer.WriteIndentedLine("public {0}(", name);

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("global::System.IServiceProvider services,");
                _writer.WriteIndentedLine("global::GreenDonut.IBatchScheduler batchScheduler,");
                _writer.WriteIndentedLine("global::GreenDonut.DataLoaderOptions options)");
                _writer.WriteIndentedLine(": base(batchScheduler, options)");
            }
        }
        else
        {
            _writer.WriteIndentedLine("public {0}(", name);

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("global::System.IServiceProvider services,");
                _writer.WriteIndentedLine("global::GreenDonut.DataLoaderOptions options)");
                _writer.WriteIndentedLine(": base(options)");
            }
        }

        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("_services = services ??");

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("throw new global::System.ArgumentNullException(nameof(services));");
            }
        }

        _writer.WriteIndentedLine("}");
    }

    public void WriteDataLoaderLoadMethod(
        string containingType,
        string methodName,
        bool isScoped,
        DataLoaderKind kind,
        ITypeSymbol key,
        ITypeSymbol value,
        Dictionary<int, string> services,
        int parameterCount,
        int cancelIndex)
    {
        if (kind is DataLoaderKind.Batch)
        {
            _writer.WriteIndentedLine(
                "protected override async global::{0}<{1}<{2}, {3}>> LoadBatchAsync(",
                WellKnownTypes.Task,
                WellKnownTypes.ReadOnlyDictionary,
                key.ToFullyQualified(),
                value.ToFullyQualified());

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("{0}<{1}> keys,", WellKnownTypes.ReadOnlyList, key.ToFullyQualified());
                _writer.WriteIndentedLine("global::{0} ct)", WellKnownTypes.CancellationToken);
            }
        }
        else if (kind is DataLoaderKind.Group)
        {
            _writer.WriteIndentedLine(
                "protected override async global::{0}<{1}<{2}, {3}>> LoadGroupedBatchAsync(",
                WellKnownTypes.Task,
                WellKnownTypes.Lookup,
                key.ToFullyQualified(),
                value.ToFullyQualified());

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("{0}<{1}> keys,", WellKnownTypes.ReadOnlyList, key.ToFullyQualified());
                _writer.WriteIndentedLine("global::{0} ct)", WellKnownTypes.CancellationToken);
            }
        }
        else if (kind is DataLoaderKind.Cache)
        {
            _writer.WriteIndentedLine(
                "protected override async global::{0}<{1}> LoadSingleAsync(",
                WellKnownTypes.Task,
                value.ToFullyQualified());

            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("{0} key,", key.ToFullyQualified());
                _writer.WriteIndentedLine("global::{0} ct)", WellKnownTypes.CancellationToken);
            }
        }

        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            if (isScoped)
            {
                _writer.WriteIndentedLine("await using var scope = _services.CreateAsyncScope();");

                foreach (var item in services.OrderBy(t => t.Key))
                {
                    _writer.WriteIndentedLine(
                        "var p{0} = scope.ServiceProvider.GetRequiredService<{1}>();",
                        item.Key,
                        item.Value);
                }
            }
            else
            {
                foreach (var item in services.OrderBy(t => t.Key))
                {
                    _writer.WriteIndentedLine(
                        "var p{0} = _services.GetRequiredService<{1}>();",
                        item.Key,
                        item.Value);
                }
            }

            _writer.WriteIndented("return await {0}.{1}(", containingType, methodName);

            for (var i = 0; i < parameterCount; i++)
            {
                if (i > 0)
                {
                    _writer.Write(", ");
                }

                if (i == 0)
                {
                    _writer.Write(
                        kind is DataLoaderKind.Cache
                            ? "key"
                            : "keys");
                }
                else if (i == cancelIndex)
                {
                    _writer.Write("ct");
                }
                else
                {
                    _writer.Write("p");
                    _writer.Write(i);
                }
            }
            _writer.WriteLine(").ConfigureAwait(false);");
        }

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
