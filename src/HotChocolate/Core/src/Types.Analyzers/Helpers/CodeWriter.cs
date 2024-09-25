using System.Text;

namespace HotChocolate.Types.Analyzers.Helpers;

public class CodeWriter : TextWriter
{
    private readonly TextWriter _writer;
    private readonly bool _disposeWriter;
    private bool _disposed;
    private int _indent;

    public CodeWriter(TextWriter writer)
    {
        _writer = writer;
        _disposeWriter = false;
    }

    public CodeWriter(StringBuilder text)
    {
        _writer = new StringWriter(text);
        _disposeWriter = true;
    }

    public override Encoding Encoding { get; } = Encoding.UTF8;

    public static string Indent { get; } = new(' ', 4);

    public override void Write(char value) =>
        _writer.Write(value);

    public void WriteStringValue(string value)
    {
        Write('"');
        Write(value);
        Write('"');
    }

    public void WriteIndent()
    {
        if (_indent > 0)
        {
            var spaces = _indent * 4;
            for (var i = 0; i < spaces; i++)
            {
                Write(' ');
            }
        }
    }

    public string GetIndentString()
    {
        if (_indent > 0)
        {
            return new string(' ', _indent * 4);
        }
        return string.Empty;
    }

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

    public void WriteIndented(string format, params object?[] args)
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
    }

    public void WriteSpace() => Write(' ');

    public IDisposable IncreaseIndent()
    {
        _indent++;
        return new Block(DecreaseIndent);
    }

    public IDisposable WriteMethod(string accessModifier, string returnType, string methodName, params string[] parameters)
    {
        WriteIndented("{0} {1} {2}(", accessModifier, returnType, methodName);

        if (parameters.Length > 0)
        {
            Write(string.Join(", ", parameters));
        }

        Write(")");
        WriteLine();
        return WithCurlyBrace();
    }

    public IDisposable WriteForEach(string item, string collection)
    {
        WriteIndentedLine("foreach(var {0} in {1})", item, collection);
        return WithCurlyBrace();
    }

    public IDisposable WriteIfClause(string condition, params object[] args)
    {
        WriteIndentedLine("if({0})", args.Length == 0 ? condition : string.Format(condition, args));
        return WithCurlyBrace();
    }

    public IDisposable WithCurlyBrace()
    {
        WriteIndentedLine("{");
        _indent++;
        return new Block(() =>
        {
            DecreaseIndent();
            WriteIndentedLine("}");
        });
    }

    public void DecreaseIndent()
    {
        if (_indent > 0)
        {
            _indent--;
        }
    }

    public IDisposable WriteBraces()
    {
        WriteLeftBrace();
        WriteLine();

        var indent = IncreaseIndent();

        return new Block(() =>
        {
            WriteLine();
            indent.Dispose();
            WriteIndent();
            WriteRightBrace();
        });
    }

    public void WriteLeftBrace() => Write('{');

    public void WriteRightBrace() => Write('}');

    public override void Flush()
    {
        base.Flush();
        _writer.Flush();
    }

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

    private sealed class Block : IDisposable
    {
        private readonly Action _decrease;

        public Block(Action close)
        {
            _decrease = close;
        }

        public void Dispose() => _decrease();
    }
}
