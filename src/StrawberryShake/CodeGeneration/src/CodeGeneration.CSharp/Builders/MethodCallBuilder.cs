namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class MethodCallBuilder : ICode
{
    private string[] _methodName = [];
    private bool _determineStatement = true;
    private bool _setNullForgiving;
    private bool _wrapArguments;
    private bool _setReturn;
    private bool _setNew;
    private bool _setAwait;
    private string? _prefix;
    private readonly List<ICode> _arguments = [];
    private readonly List<ICode> _generics = [];
    private readonly List<ICode> _chainedCode = [];

    public static MethodCallBuilder New() => new();

    public static MethodCallBuilder Inline() => New().SetDetermineStatement(false);

    public MethodCallBuilder SetPrefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    public MethodCallBuilder SetMethodName(string methodName)
    {
        _methodName =
        [
            methodName,
        ];
        return this;
    }

    public MethodCallBuilder SetMethodName(params string[] methodName)
    {
        _methodName = methodName;
        return this;
    }

    public MethodCallBuilder AddChainedCode(ICode value)
    {
        _chainedCode.Add(value);
        return this;
    }

    public MethodCallBuilder AddArgument(ICode value)
    {
        _arguments.Add(value);
        return this;
    }

    public MethodCallBuilder AddGeneric(ICode value)
    {
        _generics.Add(value);
        return this;
    }

    public MethodCallBuilder AddGeneric(string value)
    {
        _generics.Add(CodeInlineBuilder.New().SetText(value));
        return this;
    }

    public MethodCallBuilder AddArgument(string value)
    {
        _arguments.Add(CodeInlineBuilder.New().SetText(value));
        return this;
    }

    public MethodCallBuilder AddOutArgument(
        string value,
        string typeReference)
    {
        _arguments.Add(CodeInlineBuilder.New().SetText($"out {typeReference}? {value}"));
        return this;
    }

    public MethodCallBuilder SetDetermineStatement(bool value)
    {
        _determineStatement = value;
        return this;
    }

    public MethodCallBuilder SetWrapArguments(bool value = true)
    {
        _wrapArguments = value;
        return this;
    }

    public MethodCallBuilder SetNullForgiving(bool value = true)
    {
        _setNullForgiving = value;
        return this;
    }

    public MethodCallBuilder SetReturn(bool value = true)
    {
        _setReturn = value;
        return this;
    }

    public MethodCallBuilder SetNew(bool value = true)
    {
        _setNew = value;
        return this;
    }

    public MethodCallBuilder SetAwait(bool value = true)
    {
        _setAwait = value;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (_determineStatement)
        {
            writer.WriteIndent();
        }

        if (_setReturn)
        {
            writer.Write("return ");
        }

        if (_setNew)
        {
            writer.Write("new ");
        }

        if (_setAwait)
        {
            writer.Write("await ");
        }

        writer.Write(_prefix);

        if (_methodName.Length > 0)
        {
            for (var i = 0; i < _methodName.Length - 1; i++)
            {
                writer.Write(_methodName[i]);
                if (i < _methodName.Length - 2)
                {
                    writer.Write(".");
                }
            }

            if (_chainedCode.Count > 0)
            {
                writer.WriteLine();
                writer.IncreaseIndent();
                writer.WriteIndent();
            }

            if (_methodName.Length > 1)
            {
                writer.Write(".");
            }

            writer.Write(_methodName[_methodName.Length - 1]);

            if (_generics.Count > 0)
            {
                writer.Write("<");
                for (var i = 0; i < _generics.Count; i++)
                {
                    _generics[i].Build(writer);
                    if (i == _generics.Count - 1)
                    {
                        writer.Write(">");
                    }
                    else
                    {
                        writer.Write(", ");
                    }
                }
            }

            writer.Write("(");

            if (_arguments.Count == 0)
            {
                writer.Write(")");
                if (_setNullForgiving)
                {
                    writer.Write("!");
                }
            }
            else if (_arguments.Count == 1)
            {
                if (_wrapArguments)
                {
                    writer.WriteLine();
                    writer.IncreaseIndent();
                    writer.WriteIndent();
                }

                _arguments[0].Build(writer);
                if (_wrapArguments)
                {
                    writer.DecreaseIndent();
                    writer.Write(")");
                }
                else
                {
                    writer.Write(")");
                }

                if (_setNullForgiving)
                {
                    writer.Write("!");
                }
            }
            else
            {
                writer.WriteLine();

                using (writer.IncreaseIndent())
                {
                    for (var i = 0; i < _arguments.Count; i++)
                    {
                        writer.WriteIndent();
                        _arguments[i].Build(writer);
                        if (i == _arguments.Count - 1)
                        {
                            writer.Write(")");
                            if (_setNullForgiving)
                            {
                                writer.Write("!");
                            }
                        }
                        else
                        {
                            writer.Write(",");
                            writer.WriteLine();
                        }
                    }
                }
            }

            if (_chainedCode.Count > 0)
            {
                writer.DecreaseIndent();
            }
        }

        using (writer.IncreaseIndent())
        {
            foreach (var code in _chainedCode)
            {
                writer.WriteLine();
                writer.WriteIndent();
                writer.Write('.');
                code.Build(writer);
            }
        }

        if (_determineStatement)
        {
            writer.Write(";");
            writer.WriteLine();
        }
    }
}
