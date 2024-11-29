namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class TupleBuilder : ICode
{
    private bool _determineStatement = false;
    private string? _prefix;
    private bool _setReturn;
    private readonly List<ICode> _members = [];

    public static TupleBuilder New() => new();

    public static TupleBuilder Inline() => New().SetDetermineStatement(false);

    public TupleBuilder AddMember(string value)
    {
        _members.Add(CodeInlineBuilder.New().SetText(value));
        return this;
    }

    public TupleBuilder AddMember(ICode value)
    {
        _members.Add(value);
        return this;
    }

    public TupleBuilder SetPrefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    public TupleBuilder SetDetermineStatement(bool value)
    {
        _determineStatement = value;
        return this;
    }

    public TupleBuilder SetReturn(bool value = true)
    {
        _setReturn = value;
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

        writer.Write(_prefix);

        writer.Write("(");

        if (_members.Count == 0)
        {
            writer.Write(")");
        }
        else if (_members.Count == 1)
        {
            _members[0].Build(writer);
            writer.Write(")");
        }
        else
        {
            writer.WriteLine();

            using (writer.IncreaseIndent())
            {
                for (var i = 0; i < _members.Count; i++)
                {
                    writer.WriteIndent();
                    _members[i].Build(writer);
                    if (i != _members.Count - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                }
            }

            writer.WriteIndent();
            writer.Write(")");
        }

        if (_determineStatement)
        {
            writer.Write(";");
            writer.WriteLine();
        }
    }
}
