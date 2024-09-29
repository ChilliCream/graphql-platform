using StrawberryShake.Properties;

namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class CodeFileBuilder : ICodeBuilder
{
    private readonly List<string> _usings = [];
    private string? _namespace;
    private readonly List<ITypeBuilder> _types = [];

    public static CodeFileBuilder New() => new();

    public CodeFileBuilder AddUsing(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException(
                Resources.CodeFileBuilder_NamespaceCannotBeNull,
                nameof(value));
        }

        _usings.Add(value);
        return this;
    }

    public CodeFileBuilder SetNamespace(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException(
                Resources.CodeFileBuilder_NamespaceCannotBeNull,
                nameof(value));
        }

        _namespace = value;
        return this;
    }

    public CodeFileBuilder AddType(ITypeBuilder value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _types.Add(value);
        return this;
    }

    public void Build(CodeWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (_types.Count == 0 && _usings.Count == 0)
        {
            return;
        }

        if (_namespace is null)
        {
            throw new CodeGeneratorException(
                Resources.CodeFileBuilder_NamespaceCannotBeNull);
        }

        BuildInternal(writer);
    }

    private void BuildInternal(CodeWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (_types.Count == 0 && _usings.Count == 0)
        {
            return;
        }

        if (_namespace is null)
        {
            throw new CodeGeneratorException(
                Resources.CodeFileBuilder_NamespaceCannotBeNull);
        }

        if (_usings.Count > 0)
        {
            foreach (var u in _usings)
            {
                writer.WriteIndentedLine($"using {u};");
            }
            writer.WriteLine();
        }

        writer.WriteIndentedLine("#nullable enable");
        writer.WriteLine();

        writer.WriteIndentedLine($"namespace {_namespace}");
        writer.WriteIndentedLine("{");

        using (writer.IncreaseIndent())
        {
            foreach (var type in _types)
            {
                type.Build(writer);
            }
        }

        writer.WriteIndentedLine("}");
    }
}
