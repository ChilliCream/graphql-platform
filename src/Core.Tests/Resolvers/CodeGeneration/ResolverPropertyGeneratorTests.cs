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
    public class ResolverPropertyGeneratorTests
    {
        [Fact]
        public void ResolverPropertyGenerator_Generate()
        {
            // arrange
            PropertyInfo property = typeof(GeneratorTestDummyResolver).GetProperties().Single();
            FieldResolverDescriptor descriptor = FieldResolverDescriptor
                .CreateCollectionProperty(new FieldReference("Foo", "bar"),
                    property.ReflectedType, typeof(GeneratorTestDummy), property);

            // act
            StringBuilder source = new StringBuilder();
            ResolverPropertyGenerator generator = new ResolverPropertyGenerator();
            string result = generator.Generate("abc", descriptor);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }
    }
}
