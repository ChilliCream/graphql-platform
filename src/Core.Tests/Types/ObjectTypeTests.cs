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
            ServiceManager services = new ServiceManager();
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
            ServiceManager services = new ServiceManager();
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
            ServiceManager services = new ServiceManager();
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

        /// <summary>
        /// For the type detection the order of the resolver or type descriptor function should not matter.
        ///
        /// descriptor.Field("test")
        ///   .Resolver<List<string>>(() => new List<string>())
        ///   .Type<ListType<StringType>>();
        ///
        /// descriptor.Field("test")
        ///   .Type<ListType<StringType>>();
        ///   .Resolver<List<string>>(() => new List<string>())
        /// </summary>
        [Fact]
        public void ObjectTypeWithDynamicField_TypeDeclarationOrderShouldNotMatter()
        {
            // act
            Schema schema = Schema.Create(c => c.RegisterType<FooType>());

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            bool hasDynamicField = type.Fields.TryGetValue("test", out Field field);
            Assert.True(hasDynamicField);
            Assert.IsType<ListType>(field.Type);
            Assert.IsType<StringType>(((ListType)field.Type).ElementType);
        }

        public class Foo
        {
            public string Description { get; } = "hello";
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Description);
                descriptor.Field("test")
                    .Resolver<List<string>>(() => new List<string>())
                    .Type<ListType<StringType>>();
            }
        }
    }
}
