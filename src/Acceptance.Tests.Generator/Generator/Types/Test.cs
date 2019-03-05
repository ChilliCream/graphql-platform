using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    internal class Test
    {
        private string _validName;

        public string GetValidName()
        {
            if (_validName == null)
            {
                _validName = Name
                    .Replace("-", "")
                    .Replace(" ", "_");
            }

            return _validName;
        }

        public string Name { get; set; }
        public Given Given { get; set; }
        public When When { get; set; }
        public Then Then { get; set; }

        public IEnumerable<Statement> CreateStatement()
        {
            yield return new Statement("// Given");
            yield return new Statement($"string query = @\"{Given.InlineQuery()}\";");

            yield return new Statement("// When");
            var whenStatement = new Statement("DocumentNode document = _parser.Parse(query);");

            // Surround any clause with another one.

            if (Then.Passes.HasValue && Then.Passes.Value)
            {
                yield return whenStatement;

                yield return new Statement("// Then");
                yield return new Statement("Assert.NotNull(document);");
            }

            if (Then.SyntaxError.HasValue && Then.SyntaxError.Value)
            {
                yield return new Statement("// Then");
                yield return new Statement("Assert.Throws<SyntaxException>(() =>");
                yield return new Statement("{");
                yield return whenStatement;
                yield return new Statement("});");
            }
        }
    }
}
