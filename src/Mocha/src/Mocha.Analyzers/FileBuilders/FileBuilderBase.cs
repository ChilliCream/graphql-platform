using System.Text;
using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers.FileBuilders;

/// <summary>
/// Provides base functionality for file builders that generate C# source code,
/// including pooled <see cref="StringBuilder"/> management, file header writing,
/// and namespace/class scaffolding.
/// </summary>
public abstract class FileBuilderBase : IFileBuilder
{
    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _disposed;

    protected FileBuilderBase(string moduleName)
    {
        ModuleName = moduleName;
        _sb = PooledObjects.GetStringBuilder();
        _writer = new CodeWriter(_sb);
    }

    /// <summary>
    /// Gets the code writer used to emit generated source code.
    /// </summary>
    protected CodeWriter Writer => _writer;

    /// <summary>
    /// Gets the module name used to prefix generated type names.
    /// </summary>
    protected string ModuleName { get; }

    /// <summary>
    /// Gets the namespace to use in the generated source file.
    /// </summary>
    protected abstract string Namespace { get; }

    /// <inheritdoc />
    public void WriteHeader()
    {
        _writer.WriteFileHeader();
    }

    /// <inheritdoc />
    public void WriteBeginNamespace()
    {
        _writer.WriteIndentedLine("namespace {0}", Namespace);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    /// <inheritdoc />
    public void WriteEndNamespace()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    /// <inheritdoc />
    public abstract void WriteBeginClass();

    /// <inheritdoc />
    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    /// <inheritdoc />
    public string ToSourceText()
    {
        _writer.Flush();
        return _sb.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _writer.Dispose();
        PooledObjects.Return(_sb);
        _sb = null!;
        _writer = null!;
        _disposed = true;
    }
}
