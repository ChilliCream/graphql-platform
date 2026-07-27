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
    private readonly string _assemblyName;
    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _disposed;

    protected FileBuilderBase(string moduleName, string assemblyName)
    {
        ModuleName = moduleName;
        _assemblyName = assemblyName;
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
    /// Gets the simple name of the assembly being compiled.
    /// </summary>
    protected string AssemblyName => _assemblyName;

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

    public string CreateGeneratedMethodName(string runtimeTypeName, string suffix)
    {
        var runtimeName = GetRuntimeName(runtimeTypeName);
        var salt = HashHelper.ComputeIdentifierSalt($"{_assemblyName}::{runtimeTypeName}::{suffix}");

        return $"__Initialize_{runtimeName}_{suffix}_{salt}";
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

    private static string GetRuntimeName(string runtimeTypeName)
    {
        var text = RemoveGlobalPrefix(runtimeTypeName);
        var separator = text.LastIndexOf('.');
        var name = separator < 0 ? text : text.Substring(separator + 1);
        var builder = new StringBuilder(name.Length);

        foreach (var character in name)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    protected static string RemoveGlobalPrefix(string typeName)
    {
        const string Prefix = "global::";

        return typeName.StartsWith(Prefix, StringComparison.Ordinal)
            ? typeName.Substring(Prefix.Length)
            : typeName;
    }
}
