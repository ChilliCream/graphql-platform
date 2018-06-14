using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectTypeTests
    {
        [Fact]
        public void IntializeExplicitFieldWithImplicitResolver()
        {
            // arrange
            ServiceManager services = new ServiceManager(new DefaultServiceProvider());
            List<SchemaError> errors = new List<SchemaError>();
            SchemaContext context = new SchemaContext(services);

            // act
            ObjectType<Foo> fooType = new ObjectType<Foo>(
                d => d.Field(f => f.Description).Name("a"));
            ((INeedsInitialization)fooType).RegisterDependencies(
                context, e => errors.Add(e));
            context.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.NotNull(fooType.Fields.Values.First().Resolver);
        }

        [Fact]
        public void IntializeImpicitFieldWithImplicitResolver()
        {
            // arrange
            ServiceManager services = new ServiceManager(new DefaultServiceProvider());
            List<SchemaError> errors = new List<SchemaError>();
            SchemaContext context = new SchemaContext(services);

            // act
            ObjectType<Foo> fooType = new ObjectType<Foo>();
            ((INeedsInitialization)fooType).RegisterDependencies(
                context, e => errors.Add(e));
            context.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.NotNull(fooType.Fields.Values.First().Resolver);
        }

        [Fact]
        public void EnsureObjectTypeKindIsCorret()
        {
            // arrange
            ServiceManager services = new ServiceManager(new DefaultServiceProvider());
            List<SchemaError> errors = new List<SchemaError>();
            SchemaContext context = new SchemaContext(services);

            ObjectType<Foo> someObject = new ObjectType<Foo>(
                d => d.Field(f => f.Description).Name("a"));
            ((INeedsInitialization)someObject).RegisterDependencies(
                context, e => errors.Add(e));
            context.CompleteTypes();

            // act
            TypeKind kind = someObject.Kind;

            // assert
            Assert.Equal(TypeKind.Object, kind);
        }

        public class Foo
        {
            public string Description { get; } = "hello";
        }
    }


}
