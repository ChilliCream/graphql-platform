using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Moq;
using Xunit;

namespace HotChocolate
{
    public class TypeRegistryTests
    {
        [Fact]
        public void Test123()
        {
            // arrange
            var serviceProvider = new EmptyServiceProvider();
            var typeRegistry = new TypeRegistryNew(serviceProvider);
            var namedType = new MockType();
            typeRegistry.Types.Add(
                new ClrTypeReference(
                    typeof(MockType),
                    TypeContext.None),
                new RegisteredType(namedType));

            // act
            typeRegistry.RegisterDependency(
                namedType,
                new ClrTypeReference(
                    typeof(ObjectType<Foo>),
                    TypeContext.Output),
                TypeDependencyKind.Completed);

            // assert
            Assert.Empty(typeRegistry.ClrTypes);

        }

        private class MockType
            : TypeSystemObjectBase
            , INamedType
        {
            public TypeKind Kind => TypeKind.Object;

            public override IReadOnlyDictionary<string, object> ContextData =>
                throw new System.NotImplementedException();
        }

        public class Foo
        {
            public string Bar { get; set; }
        }
    }
}
