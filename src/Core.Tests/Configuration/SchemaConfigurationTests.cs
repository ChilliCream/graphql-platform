using System;
using System.Collections.Generic;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Configuration
{
    public class SchemaConfigurationTests
    {
        [Fact]
        public void Foo()
        {
            SchemaContext schemaContext = new SchemaContext();

            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindResolver<DummyResolverCollection>().To<Dummy>();
            configuration.Commit(schemaContext);

            Assert.NotNull(schemaContext.CreateResolver("Dummy", "a"));
            Assert.NotNull(schemaContext.CreateResolver("Dummy", "b"));
            Assert.Throws<InvalidOperationException>(
                () => schemaContext.CreateResolver("Dummy", "x"));
        }
    }

    public class DummyResolverCollection
    {
        public string GetA(Dummy dummy)
        {
            return null;
        }

        public string GetA(Dummy dummy, string a)
        {
            return null;
        }

        public string GetFoo(Dummy dummy)
        {
            return null;
        }

        public string B { get; set; }
    }

    public class Dummy
    {
        public string A { get; set; }
        public string B { get; set; }
    }
}
