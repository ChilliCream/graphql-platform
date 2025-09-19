using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

internal class HashCodeBuilder : ICode
{
    public const string VariableName = "hash";
    public const int Prime = 397;

    private readonly List<ICode> _code = [];

    public static HashCodeBuilder New() => new();

    public HashCodeBuilder AddCode(ICode code)
    {
        _code.Add(code);
        return this;
    }

    public void Build(CodeWriter writer)
    {
        writer.WriteIndentedLine("unchecked");
        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            writer.WriteIndentedLine($"int {VariableName} = 5;");
            writer.WriteLine();
            foreach (var check in _code)
            {
                check.Build(writer);
                writer.WriteLine();
            }

            writer.WriteIndentedLine($"return {VariableName};");
        }

        writer.WriteIndentedLine("}");
    }
}
