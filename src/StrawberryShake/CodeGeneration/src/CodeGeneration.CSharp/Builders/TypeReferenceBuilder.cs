using System.Text;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.CSharp.Builders;

public class TypeReferenceBuilder : ICode
{
    private readonly List<TypeKindToken> _buildOrder = [];
    private string? _name;
    private readonly List<string> _genericTypeArguments = [];
    private bool _skipTrailingSpace;

    public static TypeReferenceBuilder New()
    {
        return new();
    }

    public TypeReferenceBuilder SetName(string name)
    {
        _name = name;
        return this;
    }

    public TypeReferenceBuilder SkipTrailingSpace()
    {
        _skipTrailingSpace = true;
        return this;
    }

    public TypeReferenceBuilder SetNameSpace(string @namespace)
    {
        return this;
    }

    public TypeReferenceBuilder SetListType()
    {
        _buildOrder.Push(TypeKindToken.List);
        return this;
    }

    public TypeReferenceBuilder AddGeneric(string name)
    {
        _genericTypeArguments.Push(name);
        return this;
    }

    public TypeReferenceBuilder SetIsNullable(bool isNullable)
    {
        if (isNullable)
        {
            _buildOrder.Push(TypeKindToken.Nullable);
        }
        return this;
    }

    private enum TypeKindToken
    {
        List,
        Nullable,
    }

    public override string ToString()
    {
        var text = new StringBuilder();
        using var stringWriter = new StringWriter(text);
        using var codeWriter = new CodeWriter(stringWriter);
        Build(codeWriter);
        codeWriter.Flush();
        stringWriter.Flush();
        return text.ToString();
    }

    public void Build(CodeWriter writer)
    {
        HandleQueue(writer, 0);
        if (!_skipTrailingSpace)
        {
            writer.WriteSpace();
        }
    }

    private void HandleQueue(CodeWriter writer, int currentIndex)
    {
        if (currentIndex >= _buildOrder.Count)
        {
            writer.Write(_name);
            if (_genericTypeArguments.Count > 0)
            {
                writer.Write("<");
                var next = false;
                foreach (var generic in _genericTypeArguments)
                {
                    if (next)
                    {
                        writer.Write(", ");
                    }
                    next = true;

                    writer.Write(generic);
                }
                writer.Write(">");
            }

            return;
        }

        var token = _buildOrder[currentIndex];
        switch (token)
        {
            case TypeKindToken.List:
                writer.Write(TypeNames.GenericCollectionsNamespace + "IReadOnlyList<");
                HandleQueue(writer, currentIndex + 1);
                writer.Write(">");
                break;
            case TypeKindToken.Nullable:
                HandleQueue(writer, currentIndex + 1);
                writer.Write("?");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
