using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generator.ClassGenerator
{
    public class ClassMethod : IClassPart
    {
        private readonly List<Statement> _statements = new List<Statement>();
        private readonly string _returnType;
        private readonly string _name;

        public ClassMethod(
            string returnType,
            string name,
            IEnumerable<Statement> statements)
        {
            _returnType = returnType;
            _name = name;
            _statements.AddRange(statements);
        }

        public string Generate()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"[Fact]");
            builder.AppendLine($"public {_returnType} {_name}()");
            builder.AppendLine("{");

            foreach (Statement statement in _statements)
            {
                builder.AppendLine(statement.Generate());
            }

            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}