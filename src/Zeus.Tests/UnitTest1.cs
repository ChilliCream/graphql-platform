using System;
using Xunit;

namespace Zeus.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            QuerySyntaxProcessor f = new QuerySyntaxProcessor(null);
            f.Foo("{ applications { name } }");
        }

        [Fact]
        public void Test2()
        {
            QuerySyntaxProcessor f = new QuerySyntaxProcessor(null);
            f.Foo("query x($a: String) { applications { name } }");
        }

        [Fact]
        public void Test3()
        {
            QuerySyntaxProcessor f = new QuerySyntaxProcessor(null);
            f.Foo("query x($a: String) { applications(a: $a) { name } }");
        }
    }
}
