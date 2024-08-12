namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class FieldBuilder : ICodeBuilder
{
    private AccessModifier _accessModifier = AccessModifier.Private;
    private bool _isConst;
    private bool _isStatic;
    private bool _isReadOnly;
    private TypeReferenceBuilder? _type;
    private string? _name;
    private ICode? _value;
    private bool _useDefaultInitializer;
    private bool _beginValueWithNewline;

    public static FieldBuilder New() => new FieldBuilder();

    public FieldBuilder SetAccessModifier(AccessModifier value)
    {
        _accessModifier = value;
        return this;
    }

    public FieldBuilder SetType(string value, bool condition = true)
    {
        if (condition)
        {
            _type = TypeReferenceBuilder.New().SetName(value);
        }

        return this;
    }

    public FieldBuilder SetType(TypeReferenceBuilder typeReference)
    {
        _type = typeReference;
        return this;
    }

    public FieldBuilder SetName(string value)
    {
        _name = value;
        return this;
    }

    public FieldBuilder SetConst()
    {
        _isConst = true;
        _isStatic = false;
        _isReadOnly = false;
        return this;
    }

    public FieldBuilder SetStatic()
    {
        _isStatic = true;
        _isConst = false;
        return this;
    }

    public FieldBuilder SetReadOnly()
    {
        _isReadOnly = true;
        _isConst = false;
        return this;
    }

    public FieldBuilder SetValue(string? value, bool beginValueWithNewline = false)
    {
        return SetValue(
            value is not null ? CodeInlineBuilder.From(value) : null,
            beginValueWithNewline);
    }

    public FieldBuilder SetValue(ICode? value, bool beginValueWithNewline = false)
    {
        _value = value;
        _beginValueWithNewline = beginValueWithNewline;
        _useDefaultInitializer = false;
        return this;
    }

    public FieldBuilder UseDefaultInitializer()
    {
        _value = null;
        _useDefaultInitializer = true;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (_type is null)
        {
            throw new ArgumentNullException(nameof(_type));
        }

        var modifier = _accessModifier.ToString().ToLowerInvariant();

        writer.WriteIndent();
        writer.Write($"{modifier} ");

        if (_isConst)
        {
            writer.Write("const ");
        }

        if (_isStatic)
        {
            writer.Write("static ");
        }

        if (_isReadOnly)
        {
            writer.Write("readonly ");
        }

        _type.Build(writer);
        writer.Write(_name);

        if (_value is { })
        {
            writer.Write(" = ");
            if (_beginValueWithNewline)
            {
                writer.WriteLine();
                using (writer.IncreaseIndent())
                {
                    writer.WriteIndent();
                }
            }

            _value.Build(writer);
        }
        else if (_useDefaultInitializer)
        {
            writer.Write($" = new {_type}()");
        }

        writer.WriteLine(";");
    }
}
