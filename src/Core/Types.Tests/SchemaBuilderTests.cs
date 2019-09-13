using System.Threading.Tasks;
using HotChocolate.Types;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using System;
using HotChocolate.Execution;
using Moq;

namespace HotChocolate
{
    public class SchemaBuilderTests
    {
        [Fact]
        public void Create_SingleType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddRootType(typeof(FooType), OperationType.Query)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddRootType_ValueTypeAsQueryType_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddRootType((Type)null, OperationType.Query);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddRootType_ValueTypeAsQueryType_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddRootType(typeof(int), OperationType.Query);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddRootType_TypeIsNonGenericBaseType_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddRootType(typeof(ObjectType), OperationType.Query);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddRootType_TypeIsNotObjectType_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddRootType(typeof(MyEnumType), OperationType.Query);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddRootType_DuplicateQueryType_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddRootType(typeof(FooType), OperationType.Query)
                .AddRootType(typeof(FooType), OperationType.Query);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddRootType_TypeIsObjectType_SchemaIsCreated()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddRootType(typeof(FooType), OperationType.Query)
                .Create();

            // assert
            ObjectType queryType = schema.GetType<ObjectType>("Foo");
            Assert.NotNull(queryType);
            Assert.Equal(queryType, schema.QueryType);
        }

        [Fact]
        public void AddRootType_MutationType_SchemaIsCreated()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddRootType(typeof(FooType), OperationType.Query)
                .AddRootType(typeof(BarType), OperationType.Mutation)
                .Create();

            // assert
            ObjectType queryType = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(queryType);
            Assert.Equal(queryType, schema.MutationType);
        }

        [Fact]
        public void AddRootType_SubscriptionType_SchemaIsCreated()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddRootType(typeof(FooType), OperationType.Query)
                .AddRootType(typeof(BarType), OperationType.Subscription)
                .Create();

            // assert
            ObjectType queryType = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(queryType);
            Assert.Equal(queryType, schema.SubscriptionType);
        }

        [Fact]
        public void AddRootType_ObjectTypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddRootType((ObjectType)null, OperationType.Query);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddRootType_ObjTypeQueryType_SchemaIsCreated()
        {
            // arrange
            var fooType = new FooType();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddRootType(fooType, OperationType.Query)
                .Create();

            // assert
            ObjectType queryType = schema.GetType<ObjectType>("Foo");
            Assert.NotNull(queryType);
            Assert.Equal(queryType, schema.QueryType);
            Assert.Equal(fooType, schema.QueryType);
        }

        [Fact]
        public void AddRootType_ObjTypeMutationType_SchemaIsCreated()
        {
            // arrange
            var fooType = new FooType();
            var barType = new BarType();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddRootType(fooType, OperationType.Query)
                .AddRootType(barType, OperationType.Mutation)
                .Create();

            // assert
            ObjectType mutationType = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(mutationType);
            Assert.Equal(mutationType, schema.MutationType);
            Assert.Equal(barType, schema.MutationType);
        }

        [Fact]
        public void AddRootType_ObjTypeSubscriptionType_SchemaIsCreated()
        {
            // arrange
            var fooType = new FooType();
            var barType = new BarType();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddRootType(fooType, OperationType.Query)
                .AddRootType(barType, OperationType.Subscription)
                .Create();

            // assert
            ObjectType subscriptionType = schema.GetType<ObjectType>("Bar");
            Assert.NotNull(subscriptionType);
            Assert.Equal(subscriptionType, schema.SubscriptionType);
            Assert.Equal(barType, schema.SubscriptionType);
        }

        [Fact]
        public void AddRootType_ObjTypeDuplicateQueryType_ArgumentException()
        {
            // arrange
            var fooType = new FooType();

            // act
            Action action = () => SchemaBuilder.New()
                .AddRootType(fooType, OperationType.Query)
                .AddRootType(fooType, OperationType.Query);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddQueryType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(typeof(FooType))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddQueryType_Generic()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddQueryType_GenericClr()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Foo>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InferQueryType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<QueryType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddMutationType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddMutationType(typeof(FooType))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddMutationType_Generic()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddMutationType<FooType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddMutationType_GenericClr()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddMutationType<Foo>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InferMutationType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<QueryType>()
                .AddType<MutationType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddSubscriptionType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddSubscriptionType(typeof(FooType))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddSubscriptionType_Generic()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddSubscriptionType<FooType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void AddSubscriptionType_GenericClr()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddSubscriptionType<Foo>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InferSubscriptionType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<QueryType>()
                .AddType<SubscriptionType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Use_MiddlewareNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .Use((FieldMiddleware)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Use_MiddlewareDelegate()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocument(sp =>
                    Utf8GraphQLParser.Parse("type Query { a: String }"))
                .Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })
                .Create();


            // assert
            schema.MakeExecutable().Execute("{ a }").MatchSnapshot();
        }

        [Fact]
        public void AddDocument_DocumentIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddDocument((LoadSchemaDocument)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDocument_MultipleDocuments_Are_Merged()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocument(sp =>
                    Utf8GraphQLParser.Parse("type Query { a: Foo }"))
                .AddDocument(sp =>
                    Utf8GraphQLParser.Parse("type Foo { a: String }"))
                .Use(next => context =>
                {
                    context.Result = "foo";
                    return Task.CompletedTask;
                })
                .Create();


            // assert
            schema.MakeExecutable().Execute("{ a { a } }").MatchSnapshot();
        }

        [Fact]
        public void AddType_TypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddType((Type)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        // TODO : review why this was wrong and check what happens if the parent
        // of the resolver functions is more specific
        [Fact]
        public void AddType_TypeIsResolverTypeByType_QueryContainsBazField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(typeof(QueryType))
                .AddType(typeof(QueryResolverOnType))
                .Create();

            // assert
            schema.MakeExecutable().Execute("{ foo }").MatchSnapshot();
        }

        [Fact]
        public void AddType_TypeIsResolverTypeByName_QueryContainsBazField()
        {
            // arrange
            string schemaSDL = "type Query { foo: String baz: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(schemaSDL)
                .AddResolver("Query", "foo", "baz")
                .AddType(typeof(QueryResolverOnName))
                .Create();

            // assert
            ObjectType queryType = schema.GetType<ObjectType>("Query");
            Assert.True(queryType.Fields.ContainsField("baz"));
        }

        [Fact]
        public void AddType_NamedTypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddType((INamedType)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddType_QueryIsAdded_SchemaIsCreated()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Resolver("bar"));

            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(queryType)
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Query");
            Assert.Equal(queryType, type);
            Assert.Equal(queryType, schema.QueryType);
        }

        [Fact]
        public void AddType_NamedTypeExtensionIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddType((INamedTypeExtension)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddType_QueryAndExtensionAreAdded_SchemaIsCreated()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Resolver("bar"));

            var queryTypeExtension = new ObjectTypeExtension(t => t
                .Name("Query")
                .Field("bar")
                .Resolver("foo"));

            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(queryType)
                .AddType(queryTypeExtension)
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Query");
            Assert.True(type.Fields.ContainsField("bar"));
        }

        [Fact]
        public void AddDirectiveType_DirectiveTypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddDirectiveType((DirectiveType)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDirectiveType_DirectiveTypeIsAdded_SchemaIsCreated()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Resolver("bar"));

            var directiveType = new DirectiveType(t => t
                .Name("foo")
                .Location(Types.DirectiveLocation.Field));

            // act
            ISchema schema = SchemaBuilder.New()
                .AddType(queryType)
                .AddDirectiveType(directiveType)
                .Create();

            // assert
            DirectiveType type = schema.GetDirectiveType("foo");
            Assert.Equal(directiveType, type);
        }

        [Fact]
        public void SetSchema_TypeIsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .SetSchema((Type)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void SetSchema_TypeIsNotSchema_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .SetSchema(typeof(SchemaBuilderTests));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void SetSchema_TypeSchema_SchemaIsCreatedFromType()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Resolver("bar"));

            // act
            ISchema schema = SchemaBuilder.New()
                .SetSchema(typeof(MySchema))
                .AddType(queryType)
                .Create();

            // assert
            Assert.Equal("Description",
                Assert.IsType<MySchema>(schema).Description);
        }

        [Fact]
        public void SetSchema_SetName()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Resolver("bar"));

            var schemaDef = new Schema(t => t
                .Name("FooBar"));

            // act
            ISchema schema = SchemaBuilder.New()
                .SetSchema(schemaDef)
                .AddQueryType(queryType)
                .Create();

            // assert
            Assert.Equal(schemaDef, schema);
            Assert.Equal("FooBar", schema.Name);
        }

        [Fact]
        public void SetSchema_SetDescription()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Resolver("bar"));

            var schemaDef = new Schema(t => t
                .Description("TestMe"));

            // act
            ISchema schema = SchemaBuilder.New()
                .SetSchema(schemaDef)
                .AddQueryType(queryType)
                .Create();

            // assert
            Assert.Equal(schemaDef, schema);
            Assert.Equal("TestMe", schema.Description);
        }

        [Fact]
        public void SetSchema_NameDoesNotCollideWithTypeName()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("TestMe")
                .Field("foo")
                .Resolver("bar"));

            var schemaDef = new Schema(t => t
                .Name("TestMe"));

            // act
            ISchema schema = SchemaBuilder.New()
                .SetSchema(schemaDef)
                .AddQueryType(queryType)
                .Create();

            // assert
            Assert.Equal(schemaDef, schema);
            Assert.Equal("TestMe", schema.Name);
            Assert.NotNull(schema.GetType<ObjectType>("TestMe"));
        }

        [Fact]
        public void SetSchema_SchemaIsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .SetSchema((ISchema)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void SetSchema_SchemaIsNotTypeSystemObject_ArgumentException()
        {
            // arrange
            var schemaMock = new Mock<ISchema>();

            // act
            Action action = () => SchemaBuilder.New()
                .SetSchema(schemaMock.Object);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void ModifyOptions_Configure_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .ModifyOptions(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void ModifyOptions_SetQueryTypeName_SpecifTypeBecomesQueryType()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("TestMe")
                .Field("foo")
                .Resolver("bar"));

            // act
            ISchema schema = SchemaBuilder.New()
                .ModifyOptions(o => o.QueryTypeName = "TestMe")
                .AddType(queryType)
                .Create();

            // assert
            Assert.Equal("TestMe", schema.QueryType.Name);
        }

        [Fact]
        public void AddResolver_ResolverIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
               .AddResolver((FieldResolver)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddResolver_Resolver_ResolverIsSet()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("TestMe")
                .Field("foo")
                .Type<StringType>());

            FieldResolverDelegate resolverDelegate =
                c => Task.FromResult<object>(null);
            var resolverDescriptor =
                new FieldResolver("TestMe", "foo", resolverDelegate);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(queryType)
                .AddResolver(resolverDescriptor)
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("TestMe");
            Assert.NotNull(type);
            Assert.Equal(resolverDelegate, type.Fields["foo"].Resolver);
        }

        [Fact]
        public void BindClrType_IntToString_IntFieldIsStringField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithIntField>()
                .BindClrType<int, StringType>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void BindClrType_BuilderIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilderExtensions.BindClrType<int, StringType>(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindClrType_ClrTypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilder.New().BindClrType(null, typeof(StringType));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindClrType_SchemaTypeIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilder.New().BindClrType(typeof(string), null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void BindClrType_SchemaTypeIsNotTso_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () =>
                SchemaBuilder.New().BindClrType(typeof(string), typeof(string));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Dynamic_Types_Are_Integrated()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Type(new DynamicFooType("MyFoo"))
                .Resolver(new object()));

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(queryType)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void DuplicateName()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Type(new DynamicFooType("MyFoo"))
                .Resolver(new object()));

            // act
            Action action = () => SchemaBuilder.New()
                .AddQueryType(queryType)
                .AddType(new DynamicFooType("MyFoo"))
                .AddType(new DynamicFooType("MyBar"))
                .Create();

            // assert
            Assert.Equal(
                "The name `MyFoo` was already registered by another type. " +
                "- Type: MyFoo",
                Assert.Throws<SchemaException>(action).Message);
        }

        [Fact]
        public void UseFirstRegisteredDynamicType()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("Query")
                .Field("foo")
                .Type<DynamicFooType>()
                .Resolver(new object()));

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(queryType)
                .AddType(new DynamicFooType("MyFoo"))
                .AddType(new DynamicFooType("MyBar"))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Could_Not_Resolve_Type()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddDocumentFromString("type Query { foo : Bar } scalar Bar")
                .AddResolver("Query", "foo", "bar")
                .Create();

            // assert
            Assert.Equal(
                "Unable to resolve type reference `Output: Bar`. " +
                "- Type: Query",
                Assert.Throws<SchemaException>(action).Message);
        }

        [Fact]
        public void Interface_Without_Implementation()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        foo : Bar
                    }
                    interface Bar {
                        baz: String
                    }")
                .AddResolver("Query", "foo", "bar")
                .Create();

            // assert
            Assert.Equal(
                "There is no object type implementing interface `Bar`. - Type: Bar",
                Assert.Throws<SchemaException>(action).Message);
        }

        [Fact]
        public void Interface_Without_Implementation_But_Not_Used()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        foo : Baz
                    }

                    type Baz {
                        baz: String
                    }

                    interface Bar {
                        baz: String
                    }")
                .AddResolver("Query", "foo", "bar")
                .AddResolver("Baz", "baz", "baz")
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Interface_Without_Implementation_Not_Strict()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        foo : Bar
                    }
                    interface Bar {
                        baz: String
                    }")
                .AddResolver("Query", "foo", "bar")
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            Assert.NotNull(schema);
        }

        [Fact]
        public async Task Execute_Agains_Interface_Without_Impl_Field()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        foo : Bar
                    }
                    interface Bar {
                        baz: String
                    }")
                .AddResolver("Query", "foo", "bar")
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync("{ foo { baz } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public void Abstract_Classes_Are_Allowed_As_Object_Types()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<AbstractQuery>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class DynamicFooType
            : ObjectType
        {
            private NameString _typeName;

            public DynamicFooType(NameString typeName)
            {
                _typeName = typeName;
            }

            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name(_typeName);
                descriptor.Field("bar").Resolver("baz");
            }
        }

        public class QueryType
            : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("foo").Type<StringType>().Resolver("bar");
            }
        }

        public class MutationType
            : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Mutation");
                descriptor.Field("bar").Type<IntType>().Resolver(123);
            }
        }

        public class SubscriptionType
            : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Subscription");
                descriptor.Field("onFoo").Type<IntType>().Resolver(123);
            }
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar).Type<NonNullType<BarType>>();
            }
        }

        public class BarType
            : ObjectType<Bar>
        {
        }

        public class Foo
        {
            public Bar Bar { get; }
        }

        public class Bar
        {
            public string Baz { get; }
        }

        [GraphQLResolverOf(typeof(QueryType))]
        public class QueryResolverOnType
        {
            public string GetFoo([Parent]object o) => "QueryResolverOnType";
        }

        [GraphQLResolverOf("Query")]
        public class QueryResolverOnName
        {
            public string Baz { get; }
        }

        public class MySchema
            : Schema
        {
            protected override void Configure(ISchemaTypeDescriptor descriptor)
            {
                descriptor.Description("Description");
            }
        }

        public class MyEnumType
            : EnumType
        { }

        public class QueryWithIntField
        {
            public int Foo { get; set; }
        }

        public abstract class AbstractQuery
        {
            public string Foo { get; set; }

            public AbstractChild Object { get; set; }
        }

        public abstract class AbstractChild
        {
            public string Foo { get; set; }
        }
    }
}
