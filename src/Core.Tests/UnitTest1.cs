using System;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using Xunit;

namespace Core.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Source source = new Source("type Simple { a: String b: [String] }");

            Parser parser = new Parser();
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor();
            visitor.Visit(parser.Parse(source));

            var x = visitor.GetTypes().ToArray();
            Assert.NotNull(x);
            Assert.NotNull(x.First().Fields);
            Console.WriteLine();
        }
    }
}
