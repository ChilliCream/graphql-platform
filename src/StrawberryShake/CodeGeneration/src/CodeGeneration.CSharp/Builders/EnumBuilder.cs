using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class EnumBuilder : ITypeBuilder
    {
        private readonly List<(string, long?)> _elements = new();
        private string? _name;
        private string? _underlyingType;

        public static EnumBuilder New() => new();

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

        public EnumBuilder AddElement(string name, long? value = null)
        {
            _elements.Add((name, value));
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteGeneratedAttribute();

            if (_underlyingType is null)
            {
                writer.WriteIndentedLine($"public enum {_name}");
            }
            else
            {
                writer.WriteIndentedLine($"public enum {_name} : {_underlyingType}");
            }

            writer.WriteIndentedLine("{");

            using (writer.IncreaseIndent())
            {
                for (var i = 0; i < _elements.Count; i++)
                {
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
}
