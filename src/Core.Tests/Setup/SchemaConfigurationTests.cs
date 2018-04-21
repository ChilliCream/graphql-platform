using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Setup
{
    public class SchemaConfigurationTests1
    {
        [Fact]
        public void S()
        {
            // arrange
            ScalarType stringType = new ScalarType(new ScalarTypeConfig { Name = "String" });
            ObjectType objectType = new ObjectType(new ObjectTypeConfig
            {
                Name = "Dummy",
                Fields = () => new Dictionary<string, Field>
                {
                    {
                        "bar",
                        new Field(new FieldConfig
                        {
                            Name = "bar",
                            Type = () => stringType
                        })
                    }
                }
            });

            SchemaContext context = new SchemaContext(
                new INamedType[] { stringType, objectType }, new Dictionary<string, ResolveType>(), null);

            // act
            SchemaConfiguration configuration = new SchemaConfiguration();
            configuration.Name<DummyObjectType>("Dummy");
            configuration.Name<DummyObjectTypeResolver>(t => t.Field(x => x.GetFooBar(It.Is<DummyObjectType>()), "bar"));
            configuration.Resolver<DummyObjectTypeResolver, DummyObjectType>();
            configuration.Commit(context);




        }

    }

    public class DummyObjectType
    {
        public string Bar { get; } = "hello";
    }

    public class DummyObjectTypeResolver
    {
        public string GetFooBar(DummyObjectType objectType)
        {
            return objectType.Bar;
        }
    }
}
