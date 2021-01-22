using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class TypeReferenceBuilder : ITypeReferenceBuilder
    {
        private string? _name;
        private readonly List<string> _genericTypeArguments = new();

        public static TypeReferenceBuilder New() => new();

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

        public void Build(CodeWriter writer, HashSet<string>? builderContext = null)
        {
            writer.Write(_name);

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

            if (builderContext is null ||
                !builderContext.Contains(WellKnownBuilderContextData.SkipNullabilityOnce))
            {
                writer.Write("?");
            }
        }
    }
}
