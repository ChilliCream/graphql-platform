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
            var services = new ServiceManager();
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext(services);

            // act
            var fooType = new ObjectType<Foo>(
                d => d.Field(f => f.Description).Name("a"));
            INeedsInitialization init = fooType;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.NotNull(fooType.Fields.First().Resolver);
        }

        [Fact]
        public void IntializeImpicitFieldWithImplicitResolver()
        {
            // arrange
            var services = new ServiceManager();
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext(services);

            // act
            var fooType = new ObjectType<Foo>();
            INeedsInitialization init = fooType;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.NotNull(fooType.Fields.First().Resolver);
        }

        [Fact]
        public void EnsureObjectTypeKindIsCorret()
        {
            // arrange
            var services = new ServiceManager();
            var errors = new List<SchemaError>();
            var context = new SchemaContext(services);

            // act
            var someObject = new ObjectType<Foo>();

            // assert
            Assert.Equal(TypeKind.Object, someObject.Kind);
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
            var schema = Schema.Create(c =>
            {
                c.Options.StrictValidation = false;
                c.RegisterType<FooType>();
            });

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            bool hasDynamicField = type.Fields.TryGetField("test", out ObjectField field);
            Assert.True(hasDynamicField);
            Assert.IsType<ListType>(field.Type);
            Assert.IsType<StringType>(((ListType)field.Type).ElementType);
        }

        [Fact]
        public void GenericObjectTypes()
        {
            // arrange
            ServiceManager services = new ServiceManager();
            List<SchemaError> errors = new List<SchemaError>();
            SchemaContext context = new SchemaContext(services);

            // act
            ObjectType<GenericFoo<string>> genericType =
                new ObjectType<GenericFoo<string>>();
            ((INeedsInitialization)genericType).RegisterDependencies(
                context, e => errors.Add(e));
            context.CompleteTypes();

            // assert
            Assert.Equal("GenericFooOfString", genericType.Name);
        }

         [Fact]
        public void NestedGenericObjectTypes()
        {
            // arrange
            ServiceManager services = new ServiceManager();
            List<SchemaError> errors = new List<SchemaError>();
            SchemaContext context = new SchemaContext(services);

            // act
            ObjectType<GenericFoo<GenericFoo<string>>> genericType =
                new ObjectType<GenericFoo<GenericFoo<string>>>();
            ((INeedsInitialization)genericType).RegisterDependencies(
                context, e => errors.Add(e));
            context.CompleteTypes();

            // assert
            Assert.Equal("GenericFooOfGenericFooOfString", genericType.Name);
        }

        public class GenericFoo<T>
        {
            public T Value { get; }
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
                    .Resolver(() => new List<string>())
                    .Type<ListType<StringType>>();
            }
        }
    }
}
