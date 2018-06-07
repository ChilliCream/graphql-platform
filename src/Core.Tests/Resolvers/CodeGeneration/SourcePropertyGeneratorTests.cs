using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class SourcePropertyGeneratorTests
    {
        [Fact]
        public void SourcePropertyGenerator_Generate()
        {
            // arrange
            PropertyInfo property = typeof(GeneratorTestDummy).GetProperties().Single();
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateSourceProperty(new FieldReference("Foo", "bar"),
                    property.ReflectedType, property);

            // act
            StringBuilder source = new StringBuilder();
            SourcePropertyGenerator generator = new SourcePropertyGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}
