using System.Collections.Generic;
using System.Linq;
using ChilliCream.Testing;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Moq;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectTypeTests
    {
        [Fact]
        public void IntializeExplicitFieldWithImplicitResolver()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

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
        public void IntArgumentIsInferedAsNonNullType()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            var intType = new IntType();
            schemaContext.Types.RegisterType(intType);

            // act
            var fooType = new ObjectType<QueryWithIntArg>();

            // assert
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            ((INeedsInitialization)fooType)
                .RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);

            IType argumentType = fooType.Fields["bar"]
                .Arguments.First().Type;

            Assert.NotNull(argumentType);
            Assert.True(argumentType.IsNonNullType());
            Assert.Equal(intType, argumentType.NamedType());
        }

        [Fact]
        public void FieldMiddlewareIsIntegrated()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();
            var stringType = new StringType();

            schemaContext.Types.RegisterType(stringType);
            schemaContext.Resolvers.RegisterMiddleware(next => async context =>
            {
                await next(context);

                if (context.Result is string s)
                {
                    context.Result = s.ToUpperInvariant();
                }
            });

            // act
            var fooType = new ObjectType(c =>
                c.Name("Foo").Field("bar").Resolver(() => "baz"));

            // assert
            schemaContext.Types.RegisterType(fooType);
            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            ((INeedsInitialization)fooType)
                .RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            Assert.Empty(errors);

            object resolverResult = fooType.Fields["bar"]
                .Resolver(resolverContext.Object).Result;

            Assert.Equal("BAZ", resolverResult);
        }

        [Fact]
        public void IntializeImpicitFieldWithImplicitResolver()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

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
            var errors = new List<SchemaError>();
            var context = new SchemaContext();

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
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var genericType = new ObjectType<GenericFoo<string>>();
            INeedsInitialization init = genericType;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), genericType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();
            // assert
            Assert.Equal("GenericFooOfString", genericType.Name);
        }

        [Fact]
        public void NestedGenericObjectTypes()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var genericType = new ObjectType<GenericFoo<GenericFoo<string>>>();
            INeedsInitialization init = genericType;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), genericType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            // assert
            Assert.Equal("GenericFooOfGenericFooOfString", genericType.Name);
        }

        [Fact]
        public void BindFieldToResolverTypeField()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var fooType = new ObjectType<Foo>(
                d => d.Field<FooResolver>(t => t.GetBar(default)));
            INeedsInitialization init = fooType;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.Equal("foo", fooType.Fields["bar"].Arguments.First().Name);
            Assert.NotNull(fooType.Fields["bar"].Resolver);
            Assert.IsType<StringType>(fooType.Fields["bar"].Type);
        }


        [Fact]
        public void TwoInterfacesProvideFieldAWithDifferentOutputType()
        {
            // arrange
            string source = @"
                interface A {
                    a: String
                }

                interface B {
                    a: Int
                }

                type C implements A & B {
                    a: String
                }";

            // act
            try
            {
                Schema.Create(source, c =>
                {
                    c.BindResolver(() => "foo").To("C", "a");
                });
            }
            catch (SchemaException ex)
            {
                ex.Message.Snapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void TwoInterfacesProvideFieldAWithDifferentArguments1()
        {
            // arrange
            string source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(b: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

            // act
            try
            {
                Schema.Create(source, c =>
                {
                    c.BindResolver(() => "foo").To("C", "a");
                });
            }
            catch (SchemaException ex)
            {
                ex.Message.Snapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void TwoInterfacesProvideFieldAWithDifferentArguments2()
        {
            // arrange
            string source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: Int): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

            // act
            try
            {
                Schema.Create(source, c =>
                {
                    c.BindResolver(() => "foo").To("C", "a");
                });
            }
            catch (SchemaException ex)
            {
                ex.Message.Snapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void TwoInterfacesProvideFieldAWithDifferentArguments3()
        {
            // arrange
            string source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String, b: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

            // act
            try
            {
                Schema.Create(source, c =>
                {
                    c.BindResolver(() => "foo").To("C", "a");
                });
            }
            catch (SchemaException ex)
            {
                ex.Message.Snapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void ObjectFieldDoesNotMatchInterfaceDefinitionArgTypeInvalid()
        {
            // arrange
            string source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: String!): String
                }";

            // act
            try
            {
                Schema.Create(source, c =>
                {
                    c.BindResolver(() => "foo").To("C", "a");
                });
            }
            catch (SchemaException ex)
            {
                ex.Message.Snapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void ObjectFieldDoesNotMatchInterfaceDefinitionReturnTypeInvalid()
        {
            // arrange
            string source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: String): String!
                }";

            // act
            try
            {
                Schema.Create(source, c =>
                {
                    c.BindResolver(() => "foo").To("C", "a");
                });
            }
            catch (SchemaException ex)
            {
                ex.Message.Snapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void ObjectTypeImplementsAllFields()
        {
            // arrange
            string source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

            // act
            Schema schema = Schema.Create(source, c =>
            {
                c.BindResolver(() => "foo").To("C", "a");
            });

            // assert
            ObjectType type = schema.GetType<ObjectType>("C");
            Assert.Equal(2, type.Interfaces.Count);
        }

        [Fact]
        public void ObjectTypeImplementsAllFieldsWithWrappedTypes()
        {
            // arrange
            string source = @"
                interface A {
                    a(a: String!): String!
                }

                interface B {
                    a(a: String!): String!
                }

                type C implements A & B {
                    a(a: String!): String!
                }";

            // act
            Schema schema = Schema.Create(source, c =>
            {
                c.BindResolver(() => "foo").To("C", "a");
            });

            // assert
            ObjectType type = schema.GetType<ObjectType>("C");
            Assert.Equal(2, type.Interfaces.Count);
        }

        [Fact]
        public void Include_TypeWithOneField_ContainsThisField()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var fooType = new ObjectType<object>(
                d => d.Include<Foo>());
            INeedsInitialization init = fooType;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.True(fooType.Fields.ContainsField("description"));
        }

        [Fact]
        public void NonNullAttribute_StringIsRewritten_NonNullStringType()
        {
            // arrange
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            // act
            var fooType = new ObjectType<Bar>();
            INeedsInitialization init = fooType;

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), fooType, false);
            init.RegisterDependencies(initializationContext);
            schemaContext.CompleteTypes();

            // assert
            Assert.Empty(errors);
            Assert.True(fooType.Fields["baz"].Type.IsNonNullType());
            Assert.Equal("String", fooType.Fields["baz"].Type.NamedType().Name);
        }

        [Fact]
        public void ObjectType_FieldDefaultValue_SerializesCorrectly()
        {
            // arrange
            var objectType = new ObjectType(t => t.Name("Bar")
                .Field("_123").Type<StringType>()
                .Resolver(() => "").Argument("_456",
                    a => a.Type<InputObjectType<Foo>>()
                        .DefaultValue(new Foo())));

            // act
            var schema = Schema.Create(t => t.RegisterQueryType(objectType));

            // assert
            schema.ToString().Snapshot();
        }

        public class GenericFoo<T>
        {
            public T Value { get; }
        }

        public class Foo
        {
            public string Description { get; } = "hello";
        }

        public class FooResolver
        {
            public string GetBar(string foo) => "hello foo";
        }

        public class QueryWithIntArg
        {
            public string GetBar(int foo) => "hello foo";
        }

        public class Bar
        {
            [GraphQLNonNullType]
            public string Baz { get; set; }
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
