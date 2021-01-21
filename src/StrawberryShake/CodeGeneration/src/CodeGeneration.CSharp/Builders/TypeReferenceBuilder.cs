using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class TypeReferenceBuilder : ICodeBuilder
    {
        private string? _name;
        private readonly List<string> _genericTypeArguments = new();
        private TypeReferenceBuilder? _listInnerType;
        private bool _isNullable;

        public static TypeReferenceBuilder New() => new();

        public TypeReferenceBuilder SetListType(TypeReferenceBuilder innerType)
        {
            _listInnerType = innerType;
            return this;
        }

        public TypeReferenceBuilder SetName(string name)
        {
            _name = name;
            return this;
        }

        public TypeReferenceBuilder AddGeneric(string name)
        {
            _genericTypeArguments.Add(name);
            return this;
        }

        public TypeReferenceBuilder SetIsNullable(bool isNullable)
        {
            _isNullable = isNullable;
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (_listInnerType is not null)
            {
                writer.Write("IReadOnlyList<");
                _listInnerType.Build(writer);
            }
            else
            {
                writer.Write(_name);
            }

            if (_genericTypeArguments.Count > 0)
            {
                writer.Write("<");
                for (var i = 0; i < _genericTypeArguments.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }
                    writer.Write(_genericTypeArguments[i]);
                }
                writer.Write(">");
            }

            if (_isNullable)
            {
                writer.Write("?");
            }

            if (_listInnerType is not null)
            {
                writer.Write(">");
                if (_listInnerType._isNullable)
                {
                    writer.Write("?");
                }
            }

            writer.WriteSpace();
        }
    }
}
