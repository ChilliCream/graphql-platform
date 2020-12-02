using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
#if NETCOREAPP2_1
using Snapshooter;
#endif
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectTypeTests
        : TypeTestBase
    {
        [Fact]
        public void InputObjectType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()
                    .Field("bar")
                    .Type<StringType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void InputObjectType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))
                    .Field("bar")
                    .Type<StringType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericInputObjectType_DynamicName()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType<SimpleInput>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()));

                c.Options.StrictValidation = false;
            });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericInputObjectType_DynamicName_NonGeneric()
        {
            // act
            var schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType<SimpleInput>(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))));

                c.Options.StrictValidation = false;
            });

            // assert
            InputObjectType type = schema.GetType<InputObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void Initialize_IgnoreProperty_PropertyIsNotInSchemaType()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Field(f => f.Id).Ignore());

            // assert
            fooType = CreateType(fooType);
            Assert.Collection(fooType.Fields,
                t => Assert.Equal("name", t.Name));
        }


        [Fact]
        public void Initialize_UnignoreProperty_PropertyIsInSchemaType()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(d =>
               {
                   d.Field(f => f.Id).Ignore();
                   d.Field(f => f.Id).Ignore(false);
               });

            // assert
            fooType = CreateType(fooType);
            Assert.Collection(fooType.Fields,
                t => Assert.Equal("id", t.Name),
                t => Assert.Equal("name", t.Name));
        }

        [Fact]
        public void ParseLiteral()
        {
            // arrange
            Schema schema = Create();
            InputObjectType inputObjectType = schema.GetType<InputObjectType>("Object1");
            ObjectValueNode literal = CreateObjectLiteral();

            // act
            object obj = inputObjectType.ParseLiteral(literal);

            // assert
            Assert.IsType<SerializationInputObject1>(obj);
            obj.MatchSnapshot();
        }

        [Fact]
        public void EnsureInputObjectTypeKindIsCorret()
        {
            // arrange
            Schema schema = Create();
            InputObjectType inputObjectType =
                schema.GetType<InputObjectType>("Object1");

            // act
            TypeKind kind = inputObjectType.Kind;

            // assert
            Assert.Equal(TypeKind.InputObject, kind);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_NameArgs()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive("foo").Field(f => f.Id).Directive("foo"));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(
                d => d.Directive(new NameString("foo"))
                    .Field(f => f.Id)
                    .Directive(new NameString("foo")));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());


            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(d => d
                    .Name("Bar")
                    .Directive(new DirectiveNode("foo"))
                    .Field(f => f.Id)
                    .Directive(new DirectiveNode("foo")));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(d => d
                .Name("Bar")
                .Directive(new FooDirective())
                .Field(f => f.Id)
                .Directive(new FooDirective()));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void GenericInputObject_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(d => d
                .Name("Bar")
                .Directive<FooDirective>()
                .Field(f => f.Id)
                .Directive<FooDirective>());

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_NameArgs()
        {
            // arrange
            // act
            var fooType = new InputObjectType(d => d
                .Name("Bar")
                .Directive("foo")
                .Field("id")
                .Type<StringType>()
                .Directive("foo"));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_NameArgs2()
        {
            // arrange
            // act
            var fooType = new InputObjectType<SimpleInput>(d => d
                .Name("Bar")
                .Directive(new NameString("foo"))
                .Field("id")
                .Type<StringType>()
                .Directive(new NameString("foo")));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_DirectiveNode()
        {
            // arrange
            // act
            var fooType = new InputObjectType(d => d
                .Name("Bar")
                .Directive(new DirectiveNode("foo"))
                .Field("id")
                .Type<StringType>()
                .Directive(new DirectiveNode("foo")));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_DirectiveClassInstance()
        {
            // arrange
            // act
            var fooType = new InputObjectType(d => d
                .Name("Bar")
                .Directive(new FooDirective())
                .Field("id")
                .Type<StringType>()
                .Directive(new FooDirective()));

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        [Fact]
        public void InputObject_AddDirectives_DirectiveType()
        {
            // arrange
            // act
            var fooType = new InputObjectType(d => d
                .Name("Bar")
                .Directive<FooDirective>()
                .Field("id")
                .Type<StringType>()
                .Directive<FooDirective>());

            // assert
            fooType = CreateType(fooType,
                b => b.AddDirectiveType<FooDirectiveType>());

            Assert.NotEmpty(fooType.Directives["foo"]);
            Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
        }

        private static ObjectValueNode CreateObjectLiteral()
        {
            return new ObjectValueNode(new List<ObjectFieldNode>
            {
                new ObjectFieldNode("foo",
                    new ObjectValueNode(new List<ObjectFieldNode>
                    {
                        new ObjectFieldNode("fooList", new ListValueNode(Array.Empty<IValueNode>()))
                    })),
                new ObjectFieldNode("bar",
                    new StringValueNode("123"))
            });
        }

        public Schema Create()
        {
            return Schema.Create(c =>
            {
                c.Options.StrictValidation = false;

                c.RegisterType(new InputObjectType<SerializationInputObject1>(d =>
                {
                    d.Name("Object1");
                    d.Field(t => t.Foo).Type<InputObjectType<SerializationInputObject2>>();
                    d.Field(t => t.Bar).Type<StringType>();
                }));

                c.RegisterType(new InputObjectType<SerializationInputObject2>(d =>
                {
                    d.Name("Object2");
                    d.Field(t => t.FooList)
                        .Type<NonNullType<ListType<InputObjectType<SerializationInputObject1>>>>();
                }));
            });
        }

        [Fact]
        public void DoNotAllow_InputTypes_OnFields()
        {
            // arrange
            // act
            Action a = () => SchemaBuilder.New()
                .AddType(new InputObjectType(t => t
                    .Name("Foo")
                    .Field("bar")
                    .Type<NonNullType<ObjectType<SimpleInput>>>()))
                .Create();

            // assert
            Assert.Throws<SchemaException>(a)
                .Errors.First().Message.MatchSnapshot();
        }

        [Fact]
        public void DoNotAllow_DynamicInputTypes_OnFields()
        {
            // arrange
            // act
            Action a = () => SchemaBuilder.New()
                .AddType(new InputObjectType(t => t
                    .Name("Foo")
                    .Field("bar")
                    .Type(new NonNullType(new ObjectType<SimpleInput>()))))
                .Create();

            // assert
            #if NETCOREAPP2_1
            Assert.Throws<SchemaException>(a)
                .Errors.First().Message.MatchSnapshot(
                    new SnapshotNameExtension("NETCOREAPP2_1"));
            #else
            Assert.Throws<SchemaException>(a)
                .Errors.First().Message.MatchSnapshot();
            #endif
        }

        [Fact]
        public void Ignore_DescriptorIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () =>
                InputObjectTypeDescriptorExtensions
                    .Ignore<SimpleInput>(null, t => t.Id);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Ignore_ExpressionIsNull_ArgumentNullException()
        {
            // arrange
            InputObjectTypeDescriptor<SimpleInput> descriptor =
                InputObjectTypeDescriptor.New<SimpleInput>(
                    DescriptorContext.Create());

            // act
            Action action = () =>
                InputObjectTypeDescriptorExtensions
                    .Ignore(descriptor, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Ignore_Id_Property()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Unignore_Id_Property()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d =>
                {
                    d.Ignore(t => t.Id);
                    d.Field(t => t.Id).Ignore(false);
                }))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Convert_Parts_Of_The_Input_Graph()
        {
            // arrange
            var services = new ServiceCollection()
                .AddSingleton<ITypeConverter, DefaultTypeConverter>()
                .AddTypeConverter<Baz, Bar>(from => new Bar { Text = from.Text })
                .AddTypeConverter<Bar, Baz>(from => new Baz { Text = from.Text })
                .BuildServiceProvider();

            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddServices(services)
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foo(a: { bar: { text: \"abc\" } }) }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public void IsInstanceOfType_ValueIsNull_True()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            bool result = type.IsInstanceOfType((object)null);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_ValueIsSimpleInput_True()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            bool result = type.IsInstanceOfType(new SimpleInput());

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_ValueIsObject_False()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            bool result = type.IsInstanceOfType(new object());

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IValueNode_IsInstanceOfType_ValueIsNull_ArgExec()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            Action action = () => type.IsInstanceOfType((IValueNode)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void IValueNode_IsInstanceOfType_ValueIsNullValueNode_True()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            bool result = type.IsInstanceOfType(NullValueNode.Default);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IValueNode_IsInstanceOfType_ValueIsObjectValueNode_True()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            bool result = type.IsInstanceOfType(new ObjectValueNode());

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IValueNode_IsInstanceOfType_ValueIsStringValueNode_False()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            bool result = type.IsInstanceOfType(new StringValueNode("foo"));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void ParseValue_ValueIsNull()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            IValueNode valueNode = type.ParseValue((object)null);

            // assert
            valueNode.Print().MatchSnapshot();
        }

        [Fact]
        public void ParseValue_ValueIsSimpleInput()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            IValueNode valueNode = type.ParseValue(
                new SimpleInput
                {
                    Id = 1,
                    Name = "foo"
                });

            // assert
            valueNode.Print().MatchSnapshot();
        }

        [Fact]
        public void ParseLiteral_ValueIsNull_ArgExec()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            Action action = () => type.ParseLiteral((IValueNode)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void ParseLiteral_ValueIsNullValueNode()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            object result = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseLiteral_ValueIsObjectValueNode()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            object result = type.ParseLiteral(
                new ObjectValueNode(
                    new ObjectFieldNode("name", new StringValueNode("foo"))));

            // assert
            Assert.Equal("foo", Assert.IsType<SimpleInput>(result).Name);
        }

        [Fact]
        public void ParseLiteral_ValueIsStringValueNode_ArgumentException()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            Action action = () => type.ParseLiteral(new StringValueNode("foo"));

            // assert
            Assert.Throws<SerializationException>(action);
        }

        [Fact]
        public void Serialize_ValueIsNull()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            object serialized = type.Serialize(null);

            // assert
            Assert.Null(serialized);
        }

        [Fact]
        public void Serialize_ValueIsSimpleInput()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            object serialized = type.Serialize(
                new SimpleInput
                {
                    Id = 1,
                    Name = "foo"
                });

            // assert
            serialized.MatchSnapshot();
        }

        [Fact]
        public void Serialize_ValueIsDictionary()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            object serialized = type.Serialize(
                new Dictionary<string, object>
                {
                    { "name", "foo" }
                });

            // assert
            serialized.MatchSnapshot();
        }

        [Fact]
        public void Deserialize_ValueIsNull()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            bool result = type.TryDeserialize(null, out object value);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void Deserialize_ValueIsDictionary()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType<SimpleInput>(d => d
                    .Ignore(t => t.Id)))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("SimpleInput");

            // act
            bool result = type.TryDeserialize(
                new Dictionary<string, object>
                {
                    { "name", "foo" }
                },
                out object value);

            // assert
            Assert.Equal("foo", Assert.IsType<SimpleInput>(value).Name);
        }

        [Fact]
        public void Deserialize_ClrTypeIsObject_ValueIsDictionary()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType(new InputObjectType(d => d
                    .Name("Bar")
                    .Field("name")
                    .Type<StringType>()))
                .Create();

            InputObjectType type =
                schema.GetType<InputObjectType>("Bar");

            // act
            bool result = type.TryDeserialize(
                new Dictionary<string, object>
                {
                    { "name", "foo" }
                },
                out object value);

            // assert
            Assert.IsType<Dictionary<string, object>>(value);
        }

        [Fact]
        public void Ignore_Fields_With_GraphQLIgnoreAttribute()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddType<InputObjectType<FooIgnored>>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Input_With_Optionals_Not_Set()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithOptionals>()
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ do(input: { baz: \"abc\" }) { isBarSet bar baz } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [InlineData("null")]
        [InlineData("\"abc\"")]
        [Theory]
        public async Task Input_With_Optionals_Set(string value)
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithOptionals>()
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ do(input: { bar: " + value + " }) { isBarSet } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Input_With_Immutable_ClrType()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithImmutables>()
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                "{ do(input: { bar: \"abc\" baz: \"def\" qux: \"ghi\" }) { bar baz qux } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public void Input_Infer_Default_Values()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("abc")
                    .Argument("def", a => a.Type<InputObjectType<InputWithDefault>>())
                    .Resolver("ghi"))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class SimpleInput
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class SerializationInputObject1
        {
            public SerializationInputObject2 Foo { get; set; }
            public string Bar { get; set; } = "Bar";
        }

        public class SerializationInputObject2
        {
            public List<SerializationInputObject1> FooList { get; set; } =
                new List<SerializationInputObject1>
            {
                new SerializationInputObject1()
            };
        }

        public class FooDirectiveType
            : DirectiveType<FooDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<FooDirective> descriptor)
            {
                descriptor.Name("foo");
                descriptor.Location(DirectiveLocation.InputObject)
                    .Location(DirectiveLocation.InputFieldDefinition);
            }
        }

        public class FooDirective { }

        public class QueryType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("foo")
                    .Argument("a", a => a.Type<FooInputType>())
                    .Type<StringType>()
                    .Resolver(ctx => ctx.ArgumentValue<Foo>("a").Bar.Text);
            }
        }

        public class FooInputType
            : InputObjectType<Foo>
        {
            protected override void Configure(
                IInputObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar).Type<BazInputType>();
            }
        }

        public class BazInputType
            : InputObjectType<Baz>
        {
        }

        public class Foo
        {
            public Bar Bar { get; set; }
        }

        public class FooIgnored
        {
            [GraphQLIgnore]
            public Bar Bar { get; set; }

            public Bar Baz { get; set; }
        }

        public class Bar
        {
            public string Text { get; set; }
        }

        public class Baz
        {
            public string Text { get; set; }
        }

        public class QueryWithOptionals
        {
            public FooPayload Do(FooInput input)
            {
                return new FooPayload
                {
                    IsBarSet = input.Bar.HasValue,
                    Bar = input.Bar,
                    Baz = input.Baz
                };
            }
        }

        public class FooInput
        {
            public Optional<string> Bar { get; set; }
            public string Baz { get; set; }
        }

        public class FooPayload
        {
            public bool IsBarSet { get; set; }
            public string Bar { get; set; }
            public string Baz { get; set; }
        }

        public class QueryWithImmutables
        {
            public FooImmutable Do(FooImmutable input)
            {
                return input;
            }
        }

        public class FooImmutable
        {
            public FooImmutable()
            {
                Bar = "default";
            }

            public FooImmutable(string bar, string baz)
            {
                Bar = bar;
                Baz = baz;
            }

            public string Bar { get; }
            public string Baz { get; set; }
            public string Qux { get; private set; }
        }

        public class InputWithDefault
        {
            [DefaultValue("abc")]
            public string WithStringDefault { get; set; }

            [DefaultValue(null)]
            public string WithNullDefault { get; set; }

            public string WithoutDefault { get; set; }
        }
    }
}
