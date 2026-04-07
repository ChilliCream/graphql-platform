using System.Text;

namespace Mocha.Analyzers.Utils;

/// <summary>
/// Provides a fluent <see cref="TextWriter"/> for generating indented C# source code.
/// </summary>
/// <remarks>
/// This writer tracks indentation level and provides convenience methods for writing
/// indented lines, method signatures, control flow blocks, and brace-delimited scopes.
/// Disposing the writer only disposes the underlying writer when it was internally created
/// (i.e., when constructed from a <see cref="StringBuilder"/>).
/// </remarks>
public class CodeWriter : TextWriter
{
    private readonly TextWriter _writer;
    private readonly bool _disposeWriter;

    private bool _disposed;
    private int _indent;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeWriter"/> class that writes to the
    /// specified <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="writer">The underlying text writer to write generated code to.</param>
    public CodeWriter(TextWriter writer)
    {
        _writer = writer;
        _disposeWriter = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeWriter"/> class that writes to the
    /// specified <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="text">The string builder to write generated code to.</param>
    public CodeWriter(StringBuilder text)
    {
        _writer = new StringWriter(text);
        _disposeWriter = true;
    }

    /// <inheritdoc />
    public override Encoding Encoding { get; } = Encoding.UTF8;

    /// <summary>
    /// Gets a string representing a single indentation level (four spaces).
    /// </summary>
    public static string Indent { get; } = new(' ', 4);

    /// <inheritdoc />
    public override void Write(char value) => _writer.Write(value);

    /// <summary>
    /// Writes the current indentation whitespace to the output.
    /// </summary>
    public void WriteIndent()
    {
        if (_indent > 0)
        {
            Write(GetIndentString());
        }
    }

    /// <summary>
    /// Gets a string representing the current indentation whitespace.
    /// </summary>
    /// <returns>A string of spaces matching the current indentation level, or <see cref="string.Empty"/> if at the root level.</returns>
    public string GetIndentString()
    {
        if (_indent > 0)
        {
            return new string(' ', _indent * 4);
        }

        return string.Empty;
    }

    /// <summary>
    /// Writes an indented line with optional format arguments, followed by a newline.
    /// </summary>
    /// <param name="format">The format string or literal text to write.</param>
    /// <param name="args">Optional format arguments.</param>
    public void WriteIndentedLine(string format, params object?[] args)
    {
        WriteIndent();

        if (args.Length == 0)
        {
            Write(format);
        }
        else
        {
            Write(format, args);
        }

        WriteLine();
    }

    /// <summary>
    /// Writes a single space character.
    /// </summary>
    public void WriteSpace() => Write(' ');

    /// <summary>
    /// Increases the indentation level by one and returns a disposable that restores it when disposed.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that decreases the indentation level when disposed.</returns>
    public IDisposable IncreaseIndent()
    {
        _indent++;
        return new Block(DecreaseIndent);
    }

    /// <summary>
    /// Decreases the indentation level by one, unless already at the root level.
    /// </summary>
    public void DecreaseIndent()
    {
        if (_indent > 0)
        {
            _indent--;
        }
    }

    /// <inheritdoc />
    public override void Flush()
    {
        base.Flush();
        _writer.Flush();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && _disposeWriter)
        {
            if (disposing)
            {
                _writer.Dispose();
            }

            _disposed = true;
        }
    }

    private sealed class Block(Action close) : IDisposable
    {
        public void Dispose() => close();
    }
}
