using StrawberryShake.Properties;

namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class ClassBuilder : AbstractTypeBuilder
{
    private AccessModifier _accessModifier;
    private readonly bool _isPartial = true;
    private bool _isStatic;
    private bool _isSealed;
    private bool _isAbstract;
    private string? _name;
    private XmlCommentBuilder? _xmlComment;
    private readonly List<FieldBuilder> _fields = [];
    private readonly List<ConstructorBuilder> _constructors = [];
    private readonly List<MethodBuilder> _methods = [];
    private readonly List<ICodeBuilder> _classes = [];

    public static ClassBuilder New() => new();

    public static ClassBuilder New(string className) => new ClassBuilder().SetName(className);

    public ClassBuilder SetAccessModifier(AccessModifier value)
    {
        _accessModifier = value;
        return this;
    }

    public new ClassBuilder SetName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException(
                Resources.ClassBuilder_SetName_ClassNameCannotBeNull,
                nameof(value));
        }

        _name = value;
        return this;
    }

    public new ClassBuilder AddImplements(string value)
    {
        base.AddImplements(value);
        return this;
    }

    public ClassBuilder SetComment(string? xmlComment)
    {
        if (xmlComment is not null)
        {
            _xmlComment = XmlCommentBuilder.New().SetSummary(xmlComment);
        }

        return this;
    }

    public ClassBuilder SetComment(XmlCommentBuilder? xmlComment)
    {
        if (xmlComment is not null)
        {
            _xmlComment = xmlComment;
        }

        return this;
    }

    public ClassBuilder AddConstructor(ConstructorBuilder constructor)
    {
        if (constructor is null)
        {
            throw new ArgumentNullException(nameof(constructor));
        }

        _constructors.Add(constructor);
        return this;
    }

    public ClassBuilder AddField(FieldBuilder field)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        _fields.Add(field);
        return this;
    }

    public new ClassBuilder AddProperty(PropertyBuilder property)
    {
        base.AddProperty(property);
        return this;
    }

    public ClassBuilder AddMethod(MethodBuilder method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        _methods.Add(method);
        return this;
    }

    public ClassBuilder AddClass(ClassBuilder classBuilder)
    {
        if (classBuilder is null)
        {
            throw new ArgumentNullException(nameof(classBuilder));
        }

        _classes.Add(classBuilder);
        return this;
    }

    public ClassBuilder AddClass(string @class)
    {
        if (@class is null)
        {
            throw new ArgumentNullException(nameof(@class));
        }

        _classes.Add(CodeInlineBuilder.From(@class));
        return this;
    }

    public ClassBuilder SetStatic()
    {
        _isStatic = true;
        _isSealed = false;
        _isAbstract = false;
        return this;
    }

    public ClassBuilder SetSealed()
    {
        _isStatic = false;
        _isSealed = true;
        _isAbstract = false;
        return this;
    }

    public ClassBuilder SetAbstract()
    {
        _isStatic = false;
        _isSealed = false;
        _isAbstract = true;
        return this;
    }

    public override void Build(CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        _xmlComment?.Build(writer);

        writer.WriteGeneratedAttribute();

        var modifier = _accessModifier.ToString().ToLowerInvariant();

        writer.WriteIndent();

        writer.Write($"{modifier} ");

        if (_isStatic)
        {
            writer.Write("static ");
        }
        else if (_isSealed)
        {
            writer.Write("sealed ");
        }
        else if (_isAbstract)
        {
            writer.Write("abstract ");
        }

        if (_isPartial)
        {
            writer.Write("partial ");
        }

        writer.Write("class ");
        writer.WriteLine(_name);

        if (!_isStatic && Implements.Count > 0)
        {
            using (writer.IncreaseIndent())
            {
                for (var i = 0; i < Implements.Count; i++)
                {
                    writer.WriteIndentedLine(i == 0
                        ? $": {Implements[i]}"
                        : $", {Implements[i]}");
                }
            }
        }

        writer.WriteIndentedLine("{");

        var writeLine = false;

        using (writer.IncreaseIndent())
        {
            if (_fields.Count > 0)
            {
                foreach (var builder in _fields)
                {
                    builder.Build(writer);
                }

                writeLine = true;
            }

            if (_constructors.Count > 0)
            {
                for (var i = 0; i < _constructors.Count; i++)
                {
                    if (writeLine || i > 0)
                    {
                        writer.WriteLine();
                    }

                    _constructors[i]
                        .SetTypeName(_name!)
                        .Build(writer);
                }

                writeLine = true;
            }

            if (Properties.Count > 0)
            {
                for (var i = 0; i < Properties.Count; i++)
                {
                    if (writeLine || i > 0)
                    {
                        writer.WriteLine();
                    }

                    Properties[i].Build(writer);
                }

                writeLine = true;
            }

            if (_methods.Count > 0)
            {
                for (var i = 0; i < _methods.Count; i++)
                {
                    if (writeLine || i > 0)
                    {
                        writer.WriteLine();
                    }

                    _methods[i].Build(writer);
                }
            }
        }

        foreach (var classBuilder in _classes)
        {
            classBuilder.Build(writer);
        }

        writer.WriteIndentedLine("}");
    }
}
