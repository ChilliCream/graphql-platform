using System.Threading.Tasks;
using HotChocolate.Types;
using Xunit;
using Snapshooter.Xunit;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using System;
using HotChocolate.Execution;
using Moq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types.Descriptors;

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
        public void SetSchema_ConfigureIsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .SetSchema((Action<ISchemaTypeDescriptor>)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void SetSchema_ConfigureInline_DescriptionIsSet()
        {
            // arrange
            var queryType = new ObjectType(t => t
                .Name("TestMe")
                .Field("foo")
                .Resolver("bar"));

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(queryType)
                .SetSchema(c => c.Description("Some Description."))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
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

        [Fact]
        public void AddInterceptor_TypeIsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddTypeInterceptor(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddInterceptor_TypeIsNotAnInterceptorType_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddTypeInterceptor(typeof(string));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddInterceptor_TypeIsInterceptor_TypesAreTouched()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddTypeInterceptor(typeof(MyInterceptor))
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            Assert.Collection(schema.GetType<ObjectType>("Query").ContextData,
                item => Assert.Equal("touched", item.Key));

            Assert.Collection(schema.GetType<StringType>("String").ContextData,
                item => Assert.Equal("touched", item.Key));
        }

        [Fact]
        public void AddInterceptor_Generic_TypesAreTouched()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddTypeInterceptor<MyInterceptor>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            Assert.Collection(schema.GetType<ObjectType>("Query").ContextData,
                item => Assert.Equal("touched", item.Key));

            Assert.Collection(schema.GetType<StringType>("String").ContextData,
                item => Assert.Equal("touched", item.Key));
        }

        [Fact]
        public void AddInterceptor_AsService_TypesAreTouched()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton<ITypeInitializationInterceptor, MyInterceptor>();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            Assert.Collection(schema.GetType<ObjectType>("Query").ContextData,
                item => Assert.Equal("touched", item.Key));

            Assert.Collection(schema.GetType<StringType>("String").ContextData,
                item => Assert.Equal("touched", item.Key));
        }


        [Fact]
        public void AddConvention_TypeIsNullConcreteIsSet_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddConvention(null, new TestConvention());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddConvention_ConventionIsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddConvention(typeof(IConvention), default(TestConvention));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddConvention_ConventionTypeIsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddConvention(null, typeof(IConvention));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddConvention_ConcreteConventionTypeIsNull_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddConvention(typeof(IConvention), default(Type));

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }



        [Fact]
        public void AddConvention_ConventionHasInvalidTypeConcrete_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddConvention(typeof(IInvalidTestConvention), new TestConvention());

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddConvention_ConventionTypeHasInvalidType_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddConvention(typeof(IInvalidTestConvention), typeof(IConvention));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddConvention_ConventionHasInvalidType_ArgumentException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddConvention(typeof(IConvention), typeof(IInvalidTestConvention));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void AddConvention_WithImplementation_Generic()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention<ITestConvention>(TestConvention.Default)
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention());
            Assert.Equal(TestConvention.Default, convention);
        }

        [Fact]
        public void AddConvention_WithImplementation()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention(typeof(ITestConvention), TestConvention.Default)
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention2());
            Assert.NotNull(convention);
            Assert.Equal(TestConvention.Default, convention);
        }

        [Fact]
        public void AddConvention_NoImplementation_Generic()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention<ITestConvention, TestConvention>()
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention2());
            Assert.NotNull(convention);
            Assert.IsType<TestConvention>(convention);
        }

        [Fact]
        public void AddConvention_NoImplementation()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention(typeof(ITestConvention), typeof(TestConvention))
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention2());
            Assert.NotNull(convention);
            Assert.IsType<TestConvention>(convention);
        }


        [Fact]
        public void AddConvention_Override_TypeOverType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention(typeof(ITestConvention), typeof(TestConvention))
                .AddConvention(typeof(ITestConvention), typeof(TestConvention2))
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention());
            Assert.NotNull(convention);
            Assert.IsType<TestConvention2>(convention);
        }


        [Fact]
        public void AddConvention_ServiceDependency()
        {
            // arrange
            var services = new ServiceCollection();
            var provider = services.AddSingleton<MyInterceptor, MyInterceptor>()
                .BuildServiceProvider();
            var dependencyOfConvention = provider.GetService<MyInterceptor>();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddServices(provider)
                .AddConvention(
                    typeof(ITestConvention),
                    typeof(TestConventionServiceDependency))
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention());
            Assert.IsType<TestConventionServiceDependency>(convention);
            Assert.Equal(
                dependencyOfConvention,
                ((TestConventionServiceDependency)convention).Dependency);
        }

        [Fact]
        public void AddConvention_Through_ServiceCollection()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddTransient<ITestConvention, TestConvention>();
            ServiceProvider provider = services.BuildServiceProvider();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddServices(provider)
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention2());
            Assert.IsType<TestConvention>(convention);
        }

        [Fact]
        public void AddConvention_Through_ServiceCollection_ProvideImplementation()
        {
            // arrange
            var services = new ServiceCollection();
            var conventionImpl = new TestConvention();
            services.AddSingleton<ITestConvention>(conventionImpl);
            var provider = services.BuildServiceProvider();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddServices(provider)
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention2());
            Assert.IsType<TestConvention>(convention);
            Assert.Equal(convention, conventionImpl);
        }

        [Fact]
        public void AddConvention_Through_ServiceCollection_And_SchemaBuilderOverrides()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton<IConvention, TestConvention>();
            var provider = services.BuildServiceProvider();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddServices(provider)
                .AddConvention(typeof(IConvention), typeof(TestConvention2))
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention2());
            Assert.IsType<TestConvention2>(convention);
        }

        [Fact]
        public void AddConvention_NamingConvention()
        {
            // arrange
            var myNamingConvention = new DefaultNamingConventions();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention<INamingConventions>(myNamingConvention)
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<INamingConventions>(
                new DefaultNamingConventions());
            Assert.IsType<DefaultNamingConventions>(convention);
            Assert.Equal(convention, myNamingConvention);
            Assert.Equal(testType.Context.Naming, myNamingConvention);
        }

        [Fact]
        public void UseDefaultConvention()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddType<ConventionTestType>()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .Create();

            // assert
            var testType = schema.GetType<ConventionTestType>("ConventionTestType");
            var convention = testType.Context.GetConventionOrDefault<ITestConvention>(
                new TestConvention2());
            Assert.IsType<TestConvention2>(convention);
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

        public interface IInvalidTestConvention
        {

        }
        public interface ITestConvention : IConvention
        {

        }
        public class TestConvention2 : ITestConvention
        {
        }
        public class TestConvention : ITestConvention
        {

            public static TestConvention Default = new TestConvention();
        }
        public class TestConventionServiceDependency : ITestConvention
        {
            public TestConventionServiceDependency(MyInterceptor dependency)
            {
                Dependency = dependency;
            }

            public MyInterceptor Dependency { get; }
        }

        public class ConventionTestType : ObjectType<Foo>
        {
            public IDescriptorContext Context { get; private set; }

            protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Name("ConventionTestType");
                base.Configure(descriptor);
            }
            protected override void OnCompleteName(ICompletionContext context, ObjectTypeDefinition definition)
            {
                base.OnCompleteName(context, definition);
                Context = context.DescriptorContext;
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

        public class MyInterceptor
            : TypeInitializationInterceptor
        {
            public override void OnAfterCompleteType(
                ICompletionContext context,
                DefinitionBase definition,
                IDictionary<string, object> contextData)
            {
                contextData.Add("touched", true);
            }
        }
    }
}
