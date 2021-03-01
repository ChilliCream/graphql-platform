using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class XmlCommentBuilder : ICodeBuilder
    {
        private string? _summary;
        private List<string> _code = new();

        public XmlCommentBuilder SetSummary(string summary)
        {
            _summary = summary;
            return this;
        }

        public XmlCommentBuilder AddCode(string code)
        {
            _code.Add(code);
            return this;
        }

        public static XmlCommentBuilder New() => new();

        /// <summary>
        ///
        /// </summary>
        /// <param name="writer"></param>
        public void Build(CodeWriter writer)
        {
            if (_summary is not null)
            {
                writer.WriteIndentedLine("/// <summary>");
                WriteCommentLines(writer, _summary);

                foreach (var code in _code)
                {
                    writer.WriteIndentedLine("/// <code>");
                    WriteCommentLines(writer, code);
                    writer.WriteIndentedLine("/// </code>");
                }

                writer.WriteIndentedLine("/// </summary>");
            }
        }

        private void WriteCommentLines(CodeWriter writer, string str)
        {
            foreach (var line in ReadLines(str))
            {
                writer.WriteIndent();
                writer.Write("/// ");
                writer.Write(line);
                writer.WriteLine();
            }
        }

        private IEnumerable<string> ReadLines(string str)
        {
            return str.Split(new[]
                {
                    Environment.NewLine
                },
                StringSplitOptions.None);
        }
    }
}
