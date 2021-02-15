using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ArrayBuilder : ICodeBuilder
    {
        private string? _prefix;
        private string? _type;
        private bool _determineStatement = true;
        private readonly List<ICode> _assigment = new();

        private ArrayBuilder()
        {
        }

        public ArrayBuilder SetType(string type)
        {
            _type = type;
            return this;
        }

        public ArrayBuilder AddAssigment(ICode code)
        {
            _assigment.Add(code);
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

            writer.Write(_prefix);

            writer.Write("new ");
            writer.Write(_type);
            writer.Write("[] {");
            writer.WriteLine();

            using (writer.IncreaseIndent())
            {
                for (var i = 0; i < _assigment.Count; i++)
                {
                    writer.WriteIndent();
                    _assigment[i].Build(writer);
                    if (i != _assigment.Count - 1)
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
}
