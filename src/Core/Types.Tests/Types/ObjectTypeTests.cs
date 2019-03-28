using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectTypeTests
        : TypeTestBase
    {
        [Fact]
        public void IntializeExplicitFieldWithImplicitResolver()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(d => d
                .Field(f => f.Description)
                .Name("a")));

            // assert
            Assert.NotNull(fooType.Fields["a"].Resolver);
        }

        [Fact]
        public void IntArgumentIsInferedAsNonNullType()
        {
            // arrange
            // act
            ObjectType<QueryWithIntArg> fooType =
                CreateType(new ObjectType<QueryWithIntArg>());

            // assert
            IType argumentType = fooType.Fields["bar"]
                .Arguments.First().Type;

            Assert.NotNull(argumentType);
            Assert.True(argumentType.IsNonNullType());
            Assert.Equal("Int", argumentType.NamedType().Name.Value);
        }

        [Fact]
        public async Task FieldMiddlewareIsIntegrated()
        {
            // arrange
            var resolverContext = new Mock<IMiddlewareContext>();
            resolverContext.SetupAllProperties();

            // act
            ObjectType fooType = CreateType(new ObjectType(c => c
                .Name("Foo")
                .Field("bar")
                .Resolver(() => "baz")),
                b => b.Use(next => async context =>
                {
                    await next(context);

                    if (context.Result is string s)
                    {
                        context.Result = s.ToUpperInvariant();
                    }
                }));

            // assert
            await fooType.Fields["bar"].Middleware(resolverContext.Object);
            Assert.Equal("BAZ", resolverContext.Object.Result);
        }

        [Fact]
        public void IntializeImpicitFieldWithImplicitResolver()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>());

            // assert
            Assert.NotNull(fooType.Fields.First().Resolver);
        }

        [Fact]
        public void EnsureObjectTypeKindIsCorret()
        {
            // arrange
            // act
            ObjectType<Foo> someObject = CreateType(new ObjectType<Foo>());

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
        public void ObjectTypeWithDynamicField_TypeDeclaOrderShouldNotMatter()
        {
            // act
            ObjectType<FooType> fooType = CreateType(new ObjectType<FooType>());

            // assert
            Assert.True(fooType.Fields.TryGetField(
                "test", out ObjectField field));
            Assert.IsType<ListType>(field.Type);
            Assert.IsType<StringType>(((ListType)field.Type).ElementType);
        }

        [Fact]
        public void GenericObjectTypes()
        {
            // arrange
            // act
            ObjectType<GenericFoo<string>> genericType =
                CreateType(new ObjectType<GenericFoo<string>>());

            // assert
            Assert.Equal("GenericFooOfString", genericType.Name);
        }

        [Fact]
        public void NestedGenericObjectTypes()
        {
            // arrange
            // act
            ObjectType<GenericFoo<GenericFoo<string>>> genericType =
                CreateType(new ObjectType<GenericFoo<GenericFoo<string>>>());

            // assert
            Assert.Equal("GenericFooOfGenericFooOfString", genericType.Name);
        }

        [Fact]
        public void BindFieldToResolverTypeField()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(d => d
                .Field<FooResolver>(t => t.GetBar(default))));

            // assert
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
                ex.Message.MatchSnapshot();
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
                ex.Message.MatchSnapshot();
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
                ex.Message.MatchSnapshot();
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
                ex.Message.MatchSnapshot();
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
                ex.Message.MatchSnapshot();
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
                ex.Message.MatchSnapshot();
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
            var schema = Schema.Create(source, c =>
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
            // act
            ObjectType<object> fooType =
                CreateType(new ObjectType<object>(d => d
                    .Include<Foo>()));

            // assert
            Assert.True(fooType.Fields.ContainsField("description"));
        }

        [Fact]
        public void NonNullAttribute_StringIsRewritten_NonNullStringType()
        {
            // arrange
            // act
            ObjectType<Bar> fooType = CreateType(new ObjectType<Bar>());

            // assert
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
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task ObjectType_SourceTypeObject_BindsResolverCorrectly()
        {
            // arrange
            var objectType = new ObjectType(t => t.Name("Bar")
                .Field<FooResolver>(f => f.GetDescription(default))
                .Name("desc")
                .Type<StringType>());

            var schema = Schema.Create(t => t.RegisterQueryType(objectType));

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                new QueryRequest("{ desc }")
                {
                    InitialValue = new Foo()
                });

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void InferInterfaceImplementation()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(),
                b => b.AddType(new InterfaceType<IFoo>()));

            // assert
            Assert.IsType<InterfaceType<IFoo>>(
                fooType.Interfaces.First());
        }

        [Fact]
        public void IgnoreFieldWithShortcut()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(d =>
            {
                d.Ignore(t => t.Description);
                d.Field("foo").Type<StringType>().Resolver("abc");
            }));

            // assert
            Assert.Collection(
                fooType.Fields.Where(t => !t.IsIntrospectionField),
                t => Assert.Equal("foo", t.Name));

        }

        [Fact]
        public void IgnoreField_DescriptorIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action a = () => ObjectTypeDescriptorExtensions
                .Ignore<Foo>(null, t => t.Description);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void IgnoreField_ExpressionIsNull_ArgumentNullException()
        {
            // arrange
            var descriptor = new Mock<IObjectTypeDescriptor<Foo>>();

            // act
            Action a = () => ObjectTypeDescriptorExtensions
                .Ignore<Foo>(descriptor.Object, null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        public class GenericFoo<T>
        {
            public T Value { get; }
        }

        public class Foo
            : IFoo
        {
            public string Description { get; } = "hello";
        }

        public interface IFoo
        {
            string Description { get; }
        }

        public class FooResolver
        {
            public string GetBar(string foo) => "hello foo";

            public string GetDescription([Parent]Foo foo) => foo.Description;
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
