using System.Collections.Generic;
using System.Text;

namespace Generator.ClassGenerator
{
    public class ClassConstructor : IClassPart
    {
        public static ClassConstructor Empty { get; } =
            new ClassConstructor(string.Empty, new Statement[0]);

        private readonly string _className;
        private readonly List<Statement> _statements = new List<Statement>();

        public ClassConstructor(
            string className,
            IEnumerable<Statement> statements)
        {
            _className = className;
            _statements.AddRange(statements);
        }

        public string Generate()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"public {_className}()");
            builder.AppendLine($"{{");

            foreach (IClassPart statement in _statements)
            {
                builder.AppendLine(statement.Generate());
            }

            builder.AppendLine($"}}");

            return builder.ToString();
        }
    }
}
