namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class ConstructorBuilder : ICodeBuilder
{
    private AccessModifier _accessModifier = AccessModifier.Public;
    private string? _typeName;
    private readonly List<ParameterBuilder> _parameters = [];
    private readonly List<ICode> _lines = [];
    private readonly List<ICode> _base = [];

    public static ConstructorBuilder New() => new ConstructorBuilder();

    public ConstructorBuilder SetAccessModifier(AccessModifier value)
    {
        _accessModifier = value;
        return this;
    }

    public ConstructorBuilder SetTypeName(string value)
    {
        _typeName = value;
        return this;
    }

    public ConstructorBuilder AddParameter(ParameterBuilder value)
    {
        _parameters.Add(value);
        return this;
    }

    public bool HasParameters()
    {
        return _parameters.Any();
    }

    public ConstructorBuilder AddCode(ICode value)
    {
        _lines.Add(value);
        return this;
    }

    public ConstructorBuilder AddCode(string value)
    {
        _lines.Add(CodeLineBuilder.New().SetLine(value));
        return this;
    }

    public ConstructorBuilder AddBase(ICode value)
    {
        _base.Add(value);
        return this;
    }

    public ConstructorBuilder AddBase(string value)
    {
        _base.Add(CodeInlineBuilder.New().SetText(value));
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        var modifier = _accessModifier.ToString().ToLowerInvariant();

        writer.WriteIndent();

        writer.Write($"{modifier} {_typeName}(");

        if (_parameters.Count == 0)
        {
            writer.Write(")");
        }
        else if (_parameters.Count == 1)
        {
            _parameters[0].Build(writer);
            writer.Write(")");
        }
        else
        {
            writer.WriteLine();

            using (writer.IncreaseIndent())
            {
                for (var i = 0; i < _parameters.Count; i++)
                {
                    writer.WriteIndent();
                    _parameters[i].Build(writer);
                    if (i == _parameters.Count - 1)
                    {
                        writer.Write(")");
                    }
                    else
                    {
                        writer.Write(",");
                        writer.WriteLine();
                    }
                }
            }
        }

        if (_base.Count > 0)
        {
            writer.WriteLine();

            using (writer.IncreaseIndent())
            {
                writer.WriteIndent();
                writer.Write(": base(");
                if (_base.Count == 1)
                {
                    _base[0].Build(writer);
                    writer.Write(")");
                }
                else
                {
                    using (writer.IncreaseIndent())
                    {
                        for (var i = 0; i < _base.Count; i++)
                        {
                            writer.WriteLine();
                            writer.WriteIndent();
                            _base[i].Build(writer);
                            if (i == _base.Count - 1)
                            {
                                writer.Write(")");
                            }
                            else
                            {
                                writer.Write(",");
                            }
                        }
                    }
                }
            }
        }

        writer.WriteLine();
        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            foreach (var code in _lines)
            {
                code.Build(writer);
            }
        }

        writer.WriteIndentedLine("}");
    }
}
