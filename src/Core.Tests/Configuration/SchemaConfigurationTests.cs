using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Configuration
{
    public class SchemaConfigurationTests
    {
        [Fact]
        public void BindResolverCollectionToObjectTypeImplicitly()
        {
            SchemaContext schemaContext = new SchemaContext();

            StringType stringType = new StringType();
            ObjectType dummyType = new ObjectType(new ObjectTypeConfig
            {
                Name = "Dummy",
                Fields = () => new Dictionary<string, Field>
                {
                    { "a", new Field(new FieldConfig{ Name= "a", Type = () => stringType }) },
                    { "b", new Field(new FieldConfig{ Name= "a", Type = () => stringType }) }
                }
            });

            schemaContext.RegisterType(stringType);
            schemaContext.RegisterType(dummyType);

            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindResolver<DummyResolverCollection>().To<Dummy>();
            configuration.Commit(schemaContext);

            Assert.NotNull(schemaContext.CreateResolver("Dummy", "a"));
            Assert.NotNull(schemaContext.CreateResolver("Dummy", "b"));
            Assert.Throws<InvalidOperationException>(
                () => schemaContext.CreateResolver("Dummy", "x"));
        }

        [Fact]
        public void BindResolverCollectionToObjectTypeExplicitly()
        {
            SchemaContext schemaContext = new SchemaContext();

            StringType stringType = new StringType();
            ObjectType dummyType = new ObjectType(new ObjectTypeConfig
            {
                Name = "Dummy",
                Fields = () => new Dictionary<string, Field>
                {
                    { "a", new Field(new FieldConfig{ Name= "a", Type = () => stringType }) },
                    { "b", new Field(new FieldConfig{ Name= "b", Type = () => stringType }) }
                }
            });

            schemaContext.RegisterType(stringType);
            schemaContext.RegisterType(dummyType);

            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration
                .BindResolver<DummyResolverCollection>(BindingBehavior.Explicit)
                .To<Dummy>()
                .Resolve(t => t.A)
                .With(t => t.GetA(default, default));

            configuration.Commit(schemaContext);

            FieldResolverDelegate resolver = schemaContext.CreateResolver("Dummy", "a");
            Assert.NotNull(resolver);
            Assert.Equal("a_dummy_a", resolver(null, CancellationToken.None));
            Assert.Throws<InvalidOperationException>(
                () => schemaContext.CreateResolver("Dummy", "b"));
        }

        [Fact]
        public void DeriveResolverFromObjectType()
        {
            SchemaContext schemaContext = new SchemaContext();

            StringType stringType = new StringType();
            ObjectType dummyType = new ObjectType(new ObjectTypeConfig
            {
                Name = "Dummy",
                Fields = () => new Dictionary<string, Field>
                {
                    { "a", new Field(new FieldConfig{ Name= "a", Type = () => stringType }) },
                    { "b", new Field(new FieldConfig{ Name= "b", Type = () => stringType }) }
                }
            });

            schemaContext.RegisterType(stringType);
            schemaContext.RegisterType(dummyType);

            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.BindType<Dummy>();

            configuration.Commit(schemaContext);

            FieldResolverDelegate resolver = schemaContext.CreateResolver("Dummy", "a");
            Assert.NotNull(resolver);
            Assert.Equal("a_dummy_a", resolver(null, CancellationToken.None));
            Assert.Throws<InvalidOperationException>(
                () => schemaContext.CreateResolver("Dummy", "b"));
        }
    }

    public class DummyResolverCollection
    {
        public string GetA(Dummy dummy)
        {
            return "a_dummy";
        }

        public string GetA(Dummy dummy, string a)
        {
            return "a_dummy_a";
        }

        public string GetFoo(Dummy dummy)
        {
            return null;
        }

        public string B { get; set; }
    }

    public class Dummy
    {
        public string A { get; set; } = "a";
        public string B { get; set; } = "b";
    }
}
