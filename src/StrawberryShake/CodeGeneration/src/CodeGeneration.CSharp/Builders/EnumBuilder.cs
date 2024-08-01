namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class EnumBuilder : ITypeBuilder
{
    private AccessModifier _accessModifier;
    private readonly List<(string, long?, XmlCommentBuilder?)> _elements = [];
    private string? _name;
    private string? _underlyingType;
    private XmlCommentBuilder? _xmlComment;

    public static EnumBuilder New() => new();

    public EnumBuilder SetAccessModifier(AccessModifier value)
    {
        _accessModifier = value;
        return this;
    }

    public EnumBuilder SetName(string value)
    {
        _name = value;
        return this;
    }

    public EnumBuilder SetUnderlyingType(RuntimeTypeInfo? value)
    {
        _underlyingType = value?.ToString();
        return this;
    }

    public EnumBuilder SetUnderlyingType(string? value)
    {
        _underlyingType = value;
        return this;
    }

    public EnumBuilder AddElement(string name, long? value = null, string? documentation = null)
    {
        _elements.Add((
            name,
            value,
            documentation is null
                ? null
                : XmlCommentBuilder.New().SetSummary(documentation)));
        return this;
    }

    public EnumBuilder SetComment(string? xmlComment)
    {
        if (xmlComment is not null)
        {
            _xmlComment = XmlCommentBuilder.New().SetSummary(xmlComment);
        }

        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        _xmlComment?.Build(writer);

        writer.WriteGeneratedAttribute();

        var modifier = _accessModifier.ToString().ToLowerInvariant();

        if (_underlyingType is null)
        {
            writer.WriteIndentedLine($"{modifier} enum {_name}");
        }
        else
        {
            writer.WriteIndentedLine($"{modifier} enum {_name} : {_underlyingType}");
        }

        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            for (var i = 0; i < _elements.Count; i++)
            {
                _elements[i].Item3?.Build(writer);

                writer.WriteIndent();
                writer.Write(_elements[i].Item1);

                if (_elements[i].Item2.HasValue)
                {
                    writer.Write($" = {_elements[i].Item2}");
                }

                if (i + 1 < _elements.Count)
                {
                    writer.Write($",");
                }

                writer.WriteLine();
            }
        }

        writer.WriteIndentedLine("}");
    }
}
