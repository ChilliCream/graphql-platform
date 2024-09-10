namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class PropertyBuilder : ICodeBuilder
{
    private AccessModifier _accessModifier;
    private bool _isReadOnly = true;
    private ICode? _lambdaResolver;
    private bool _isOnlyDeclaration;
    private TypeReferenceBuilder? _type;
    private string? _name;
    private XmlCommentBuilder? _xmlComment;
    private string? _value;
    private string? _interface;
    private bool _isStatic;
    private bool _isOverride;

    public static PropertyBuilder New() => new();

    public PropertyBuilder SetAccessModifier(AccessModifier value)
    {
        _accessModifier = value;
        return this;
    }

    public PropertyBuilder SetStatic()
    {
        _isStatic = true;
        return this;
    }

    public PropertyBuilder SetOverride()
    {
        _isOverride = true;
        return this;
    }

    public PropertyBuilder AsLambda(string resolveCode)
    {
        _lambdaResolver = CodeInlineBuilder.From(resolveCode);
        return this;
    }

    public PropertyBuilder AsLambda(ICode resolveCode)
    {
        _lambdaResolver = resolveCode;
        return this;
    }

    public PropertyBuilder SetType(string value)
    {
        _type = TypeReferenceBuilder.New().SetName(value);
        return this;
    }

    public PropertyBuilder SetComment(string? xmlComment)
    {
        if (xmlComment is not null)
        {
            _xmlComment = XmlCommentBuilder.New().SetSummary(xmlComment);
        }

        return this;
    }

    public PropertyBuilder SetOnlyDeclaration(bool value = true)
    {
        _isOnlyDeclaration = value;
        return this;
    }

    public PropertyBuilder SetType(TypeReferenceBuilder value)
    {
        _type = value;
        return this;
    }

    public PropertyBuilder SetName(string value)
    {
        _name = value;
        return this;
    }

    public PropertyBuilder SetInterface(string value)
    {
        _interface = value;
        return this;
    }

    public PropertyBuilder SetValue(string? value)
    {
        _value = value;
        return this;
    }

    public PropertyBuilder MakeSettable()
    {
        _isReadOnly = false;
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

        _xmlComment?.Build(writer);

        writer.WriteIndent();
        if (_interface is null && !_isOnlyDeclaration)
        {
            writer.Write(modifier);
            writer.WriteSpace();
            if (_isStatic)
            {
                writer.Write("static");
                writer.WriteSpace();
            }

            if (_isOverride)
            {
                writer.Write("override");
                writer.WriteSpace();
            }
        }

        _type.Build(writer);

        if (_interface is not null)
        {
            writer.Write(_interface);
            writer.Write(".");
        }

        writer.Write(_name);

        if (_lambdaResolver is not null)
        {
            writer.Write(" => ");
            _lambdaResolver.Build(writer);
            writer.Write(";");
            writer.WriteLine();
            return;
        }

        writer.Write(" {");
        writer.Write(" get;");
        if (!_isReadOnly)
        {
            writer.Write(" set;");
        }

        writer.Write(" }");

        if (_value is not null)
        {
            writer.Write(" = ");
            writer.Write(_value);
            writer.Write(";");
        }

        writer.WriteLine();
    }
}
