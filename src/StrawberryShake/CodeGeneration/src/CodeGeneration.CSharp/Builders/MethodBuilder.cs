namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class MethodBuilder : ICodeContainer<MethodBuilder>
{
    private AccessModifier _accessModifier = AccessModifier.Private;
    private Inheritance _inheritance = Inheritance.None;
    private bool _isStatic;
    private bool _isOnlyDeclaration;
    private bool _isOverride;
    private bool _is;
    private TypeReferenceBuilder _returnType = TypeReferenceBuilder.New().SetName("void");
    private string? _name;
    private readonly List<ParameterBuilder> _parameters = [];
    private readonly List<ICode> _lines = [];
    private bool _isAsync;
    private string? _interface;

    public static MethodBuilder New() => new();

    public MethodBuilder SetAccessModifier(AccessModifier value)
    {
        _accessModifier = value;
        return this;
    }

    public MethodBuilder SetStatic()
    {
        _isStatic = true;
        return this;
    }

    public MethodBuilder SetAsync()
    {
        _isAsync = true;
        return this;
    }

    public MethodBuilder SetInterface(string value)
    {
        _interface = value;
        return this;
    }

    public MethodBuilder SetOverride()
    {
        _isOverride = true;
        return this;
    }

    public MethodBuilder Set()
    {
        _is = true;
        return this;
    }

    public MethodBuilder SetInheritance(Inheritance value)
    {
        _inheritance = value;
        return this;
    }

    public MethodBuilder SetOnlyDeclaration(bool value = true)
    {
        _isOnlyDeclaration = value;
        return this;
    }

    public MethodBuilder SetReturnType(string value, bool condition = true)
    {
        if (condition)
        {
            _returnType = TypeReferenceBuilder.New().SetName(value);
        }

        return this;
    }

    public MethodBuilder SetReturnType(TypeReferenceBuilder value, bool condition = true)
    {
        if (condition)
        {
            _returnType = value;
        }

        return this;
    }

    public MethodBuilder SetName(string value)
    {
        _name = value;
        return this;
    }

    public MethodBuilder AddParameter(ParameterBuilder value)
    {
        _parameters.Add(value);
        return this;
    }

    public MethodBuilder AddCode(string code, bool addIf = true)
    {
        if (addIf)
        {
            _lines.Add(CodeLineBuilder.New().SetLine(code));
        }

        return this;
    }

    public MethodBuilder AddCode(ICode code, bool addIf = true)
    {
        if (addIf)
        {
            _lines.Add(code);
        }

        return this;
    }

    public MethodBuilder AddEmptyLine()
    {
        _lines.Add(CodeLineBuilder.New());
        return this;
    }

    public MethodBuilder AddInlineCode(string code)
    {
        _lines.Add(CodeInlineBuilder.New().SetText(code));
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

        if (_interface is null && !_isOnlyDeclaration)
        {
            writer.Write($"{modifier} ");

            if (_isStatic)
            {
                writer.Write("static ");
            }

            if (_isOverride)
            {
                writer.Write("override ");
            }

            if (_isAsync)
            {
                writer.Write("async ");
            }

            if (_is)
            {
                writer.Write(" ");
            }

            writer.Write($"{CreateInheritance()}");
        }

        _returnType.Build(writer);

        if (_interface is not null)
        {
            writer.Write(_interface);
            writer.Write(".");
        }

        writer.Write($"{_name}(");

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

        if (_isOnlyDeclaration)
        {
            writer.Write(";");
            writer.WriteLine();
        }
        else
        {
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

    private string CreateInheritance()
        => _inheritance switch
        {
            Inheritance.Override => "override ",
            Inheritance.Sealed => "sealed override ",
            Inheritance.Virtual => "virtual ",
            _ => string.Empty,
        };
}
