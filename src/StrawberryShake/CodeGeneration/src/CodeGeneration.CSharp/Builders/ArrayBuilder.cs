namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class ArrayBuilder : ICode
{
    private string? _prefix;
    private string? _type;
    private bool _determineStatement = true;
    private bool _setReturn;
    private readonly List<ICode> _assignment = [];

    private ArrayBuilder()
    {
    }

    public ArrayBuilder SetType(string type)
    {
        _type = type;
        return this;
    }

    public ArrayBuilder AddAssignment(ICode code)
    {
        _assignment.Add(code);
        return this;
    }

    public ArrayBuilder SetDetermineStatement(bool value)
    {
        _determineStatement = value;
        return this;
    }

    public ArrayBuilder SetPrefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    public ArrayBuilder SetReturn(bool value = true)
    {
        _setReturn = value;
        return this;
    }

    public void Build(CodeWriter writer)
    {
        if (_type is null)
        {
            throw new ArgumentNullException(nameof(_type));
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

        writer.Write("new ");
        writer.Write(_type);
        writer.Write("[] {");
        writer.WriteLine();

        using (writer.IncreaseIndent())
        {
            for (var i = 0; i < _assignment.Count; i++)
            {
                writer.WriteIndent();
                _assignment[i].Build(writer);
                if (i != _assignment.Count - 1)
                {
                    writer.Write(",");
                    writer.WriteLine();
                }
            }
        }

        writer.WriteLine();
        writer.WriteIndent();
        writer.Write("}");

        if (_determineStatement)
        {
            writer.Write(";");
            writer.WriteLine();
        }
    }

    public static ArrayBuilder New() => new ArrayBuilder();
}
