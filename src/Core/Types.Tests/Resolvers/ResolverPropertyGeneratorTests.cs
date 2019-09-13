using System.Collections.Immutable;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Resolvers.Expressions
{
    public class ResolverCompilerTests
    {
        [Fact]
        public async Task Compile_TaskObjMethod_NoParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("ObjectTaskResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectResolverResult", result);
        }

        [Fact]
        public async Task Compile_TaskStringMethod_NoParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("StringTaskResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolver", result);
        }

        [Fact]
        public async Task Compile_TaskStringMethod_WithParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("StringTaskResolverWithArg");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.Argument<string>("a")).Returns("abc");
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task Compile_ObjMethod_NoParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("ObjectResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectResolverResult", result);
        }

        [Fact]
        public async Task Compile_StringMethod_NoParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("StringResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolver", result);
        }

        [Fact]
        public async Task Compile_StringMethod_WithParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("StringResolverWithArg");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.Argument<string>("a")).Returns("abc");
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task Compile_ObjTaskProperty_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetProperty("ObjectTaskStringProp");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectTaskStringProp", result);
        }

        [Fact]
        public async Task Compile_StringTaskProperty_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetProperty("StringTaskResolverProp");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolverProp", result);
        }

        [Fact]
        public async Task Compile_StringProperty_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetProperty("StringProp");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringProp", result);
        }

        [Fact]
        public async Task Compile_TaskObjMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("ObjectTaskResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectResolverResult", result);
        }

        [Fact]
        public async Task Compile_TaskStringMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("StringTaskResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolver", result);
        }

        [Fact]
        public async Task Compile_TaskStringMethod_WithParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("StringTaskResolverWithArg");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            context.Setup(t => t.Argument<string>("a")).Returns("abc");
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task Compile_ObjMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("ObjectResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectResolverResult", result);
        }

        [Fact]
        public async Task Compile_StringMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod("StringResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolver", result);
        }

        [Fact]
        public async Task Compile_StringMethod_WithParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("StringResolverWithArg");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            context.Setup(t => t.Argument<string>("a")).Returns("abc");
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task Compile_ObjTaskProperty_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetProperty("ObjectTaskStringProp");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectTaskStringProp", result);
        }

        [Fact]
        public async Task Compile_StringTaskProperty_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetProperty("StringTaskResolverProp");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolverProp", result);
        }

        [Fact]
        public async Task Compile_StringProperty_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetProperty("StringProp");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                typeof(Entity),
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>())
                .Returns(new Resolvers());
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringProp", result);
        }

        [Fact]
        public async Task Compile_Arguments_CancellationToken()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithCancellationToken");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.RequestAborted)
                .Returns(CancellationToken.None);
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("cancel", result);
        }

        [Fact]
        public async Task Compile_Arguments_ResolverContext()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithResolverContext");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_EventMessage()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithEventMessage");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.Setup(t => t.CustomProperty<IEventMessage>(
                typeof(IEventMessage).FullName))
                .Returns(new Mock<IEventMessage>().Object);
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_FieldSelection()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithFieldSelection");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.FieldSelection)
                .Returns(new FieldNode(
                    null,
                    new NameNode("foo"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null));
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_ObjectType()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithObjectType");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create(); ;
            ObjectType queryType = schema.GetType<ObjectType>("Query");
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.ObjectType)
                .Returns(queryType);
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_Operation()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithOperationDefinition");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.Operation)
                .Returns(new OperationDefinitionNode(
                    null,
                    null,
                    OperationType.Query,
                    Array.Empty<VariableDefinitionNode>(),
                    Array.Empty<DirectiveNode>(),
                    new SelectionSetNode(
                        null,
                        Array.Empty<ISelectionNode>())));
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_ObjectField()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithObjectField");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create(); ;
            ObjectType queryType = schema.GetType<ObjectType>("Query");
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.Field)
                .Returns(queryType.Fields.First());
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_IOutputField()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithOutputField");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create(); ;
            ObjectType queryType = schema.GetType<ObjectType>("Query");
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.Field)
                .Returns(queryType.Fields.First());
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_Document()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithDocument");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.Document)
                .Returns(new DocumentNode(
                    null,
                    Array.Empty<IDefinitionNode>()));
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_Schema()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithSchema");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create(); ;
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.Schema)
                .Returns(schema);
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_Service()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolverWithService");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.Setup(t => t.Service<MyService>())
                .Returns(new MyService());
            bool result = (bool)await resolver.Resolver(context.Object);
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_ContextData()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolveWithContextData");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = new Dictionary<string, object>
            {
                { "foo", "bar"}
            };

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("bar", result);
        }

        [Fact]
        public async Task Compile_Arguments_ContextData_DefaultValue()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolveWithContextDataDefault");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = new Dictionary<string, object>();

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            object result = await resolver.Resolver(context.Object);
            Assert.Null(result);
        }

        [Fact]
        public void Compile_Arguments_ContextData_NotExists()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolveWithContextData");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = new Dictionary<string, object>();

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            Action action = () => resolver.Resolver(context.Object);
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public async Task Compile_Arguments_ScopedContextData()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolveWithScopedContextData");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty
                .SetItem("foo", "bar");

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ScopedContextData).Returns(contextData);
            string result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("bar", result);
        }

        [Fact]
        public async Task Compile_Arguments_ScopedContextData_DefaultValue()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolveWithScopedContextDataDefault");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty;

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ScopedContextData).Returns(contextData);
            object result = await resolver.Resolver(context.Object);
            Assert.Null(result);
        }

        [Fact]
        public void Compile_Arguments_ScopedContextData_NotExists()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("ResolveWithScopedContextData");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty;

            // act
            var compiler = new ResolverCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ScopedContextData).Returns(contextData);
            Action action = () => resolver.Resolver(context.Object);
            Assert.Throws<ArgumentException>(action);
        }


        public class Resolvers
        {
            public Task<object> ObjectTaskResolver() =>
                Task.FromResult<object>("ObjectResolverResult");

            public Task<string> StringTaskResolver() =>
                Task.FromResult<string>("StringTaskResolver");

            public Task<string> StringTaskResolverWithArg(string a) =>
                Task.FromResult<string>(a);

            public object ObjectResolver() => "ObjectResolverResult";

            public string StringResolver() => "StringTaskResolver";

            public string StringResolverWithArg(string a) => a;

            public Task<object> ObjectTaskStringProp { get; } =
               Task.FromResult<object>("ObjectTaskStringProp");

            public Task<string> StringTaskResolverProp { get; } =
                Task.FromResult("StringTaskResolverProp");

            public string StringProp { get; } = "StringProp";

            public string ResolverWithCancellationToken(
                CancellationToken cancellationToken) =>
                "cancel";

            public bool ResolverWithResolverContext(
                IResolverContext context) =>
                context != null;

            public bool ResolverWithEventMessage(
                IEventMessage message) =>
                message != null;

            public bool ResolverWithFieldSelection(
                FieldNode fieldSelection) =>
                fieldSelection != null;

            public bool ResolverWithObjectType(
                ObjectType objectType) =>
                objectType != null;

            public bool ResolverWithOperationDefinition(
                OperationDefinitionNode operationDefinition) =>
                operationDefinition != null;

            public bool ResolverWithObjectField(
                ObjectField objectField) =>
                objectField != null;

            public bool ResolverWithOutputField(
                IOutputField outputField) =>
                outputField != null;

            public bool ResolverWithDocument(
                DocumentNode document) =>
                document != null;

            public bool ResolverWithSchema(
                ISchema schema) =>
                schema != null;

            public bool ResolverWithService(
                [Service]MyService service) =>
                service != null;

            public string ResolveWithContextData(
                [State("foo")]string s) => s;

            public string ResolveWithContextDataDefault(
                [State("foo", DefaultIfNotExists = true)]string s) => s;

            public string ResolveWithScopedContextData(
                [State("foo", IsScoped = true)]string s) => s;

            public string ResolveWithScopedContextDataDefault(
                [State("foo", IsScoped = true, DefaultIfNotExists = true)]
                string s) => s;
        }

        public class Entity { }

        public class MyService { }
    }
}
