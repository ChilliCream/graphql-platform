namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class InterfaceBuilder : AbstractTypeBuilder
{
    private AccessModifier _accessModifier;
    private readonly List<MethodBuilder> _methods = [];

    private XmlCommentBuilder? _xmlComment;

    public static InterfaceBuilder New() => new();

    public InterfaceBuilder SetAccessModifier(AccessModifier value)
    {
        _accessModifier = value;
        return this;
    }

    public new InterfaceBuilder SetName(string name)
    {
        base.SetName(name);
        return this;
    }

    public new InterfaceBuilder AddImplements(string value)
    {
        base.AddImplements(value);
        return this;
    }

    public new InterfaceBuilder AddProperty(PropertyBuilder property)
    {
        base.AddProperty(property);
        return this;
    }

    public InterfaceBuilder SetComment(string? xmlComment)
    {
        if (xmlComment is not null)
        {
            _xmlComment = XmlCommentBuilder.New().SetSummary(xmlComment);
        }

        return this;
    }

    public InterfaceBuilder SetComment(XmlCommentBuilder? xmlComment)
    {
        if (xmlComment is not null)
        {
            _xmlComment = xmlComment;
        }

        return this;
    }

    public InterfaceBuilder AddMethod(MethodBuilder method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        _methods.Add(method);
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

        writer.WriteIndent();

        var modifier = _accessModifier.ToString().ToLowerInvariant();

        writer.Write($"{modifier} partial interface ");
        writer.WriteLine(Name);

        if (Implements.Count > 0)
        {
            using (writer.IncreaseIndent())
            {
                for (var i = 0; i < Implements.Count; i++)
                {
                    writer.WriteIndentedLine(
                        i == 0
                            ? $": {Implements[i]}"
                            : $", {Implements[i]}");
                }
            }
        }

        writer.WriteIndentedLine("{");

        var writeLine = false;

        using (writer.IncreaseIndent())
        {
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

        writer.WriteIndentedLine("}");
    }
}
