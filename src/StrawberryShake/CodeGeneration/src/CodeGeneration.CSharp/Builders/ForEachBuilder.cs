using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ForEachBuilder : ICodeContainer<ForEachBuilder>
    {
        private string? _loopHeader;
        private readonly List<ICode> _lines = new List<ICode>();

        public static ForEachBuilder New() => new();

        public ForEachBuilder AddCode(string code, bool addIf = true)
        {
            AddCode(
                CodeLineBuilder.New().SetLine(code),
                addIf);
            return this;
        }

        public ForEachBuilder AddCode(ICode code, bool addIf = true)
        {
            if (addIf)
            {
                _lines.Add(code);
            }

            return this;
        }

        public ForEachBuilder AddEmptyLine()
        {
            _lines.Add(CodeLineBuilder.New());
            return this;
        }

        public ForEachBuilder SetLoopHeader(string elementCode)
        {
            _loopHeader = elementCode;
            return this;
        }

        public void Build(CodeWriter writer, HashSet<string>? builderContext = null)
        {
            writer.WriteIndent();
            writer.Write("foreach (");
            writer.Write(_loopHeader);
            writer.Write(")");
            writer.WriteLine();
            writer.WriteIndent();
            writer.WriteLine("{");
            using (writer.IncreaseIndent())
            {
                foreach (ICode line in _lines)
                {
                    line.Build(writer);
                }
            }

            writer.WriteIndent();
            writer.WriteLine("}");
        }
    }
}
