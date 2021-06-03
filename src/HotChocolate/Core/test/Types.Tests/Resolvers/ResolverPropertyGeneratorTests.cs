using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
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
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.ObjectTaskResolver));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
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
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.StringTaskResolver));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
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
                type.GetMethod(nameof(Resolvers.StringTaskResolverWithArg));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ArgumentValue<string>("a")).Returns("abc");
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task Compile_ObjMethod_NoParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.ObjectResolver));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectResolverResult", result);
        }

        [Fact]
        public async Task Compile_StringMethod_NoParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.StringResolver));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolver", result);
        }

        [Fact]
        public async Task Compile_StringMethod_WithParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.StringResolverWithArg));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ArgumentValue<string>("a")).Returns("abc");
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task Compile_StringValueNodeMethod_WithParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.StringValueNodeResolverWithArg));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ArgumentLiteral<StringValueNode>("a"))
                .Returns(new StringValueNode("abc"));
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task Compile_OptionalStringMethod_WithParams_SourceResolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.OptionalStringResolverWithArg));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ArgumentOptional<string>("a"))
                .Returns(new Optional<string>("abc"));
            var result = (string)await resolver.Resolver(context.Object);
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
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
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
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
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
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringProp", result);
        }

        [Fact]
        public async Task Compile_TaskObjMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.ObjectTaskResolver));
            var resolverDescriptor = new ResolverDescriptor(
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                resolverType: type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectResolverResult", result);
        }

        [Fact]
        public async Task Compile_TaskStringMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.StringTaskResolver));
            var resolverDescriptor = new ResolverDescriptor(
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                resolverType: type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolver", result);
        }

        [Fact]
        public async Task Compile_TaskStringMethod_WithParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.StringTaskResolverWithArg));
            var resolverDescriptor = new ResolverDescriptor(
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                resolverType: type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ArgumentValue<string>("a")).Returns("abc");
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task Compile_ObjMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.ObjectResolver));
            var resolverDescriptor = new ResolverDescriptor(
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                resolverType: type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("ObjectResolverResult", result);
        }

        [Fact]
        public async Task Compile_StringMethod_NoParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.StringResolver));
            var resolverDescriptor = new ResolverDescriptor(
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringTaskResolver", result);
        }

        [Fact]
        public async Task Compile_StringMethod_WithParams_Resolver()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.StringResolverWithArg));
            var resolverDescriptor = new ResolverDescriptor(
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ArgumentValue<string>("a")).Returns("abc");
            var result = (string)await resolver.Resolver(context.Object);
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
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                resolverType: type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
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
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                resolverType: type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
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
                typeof(Entity),
                new FieldMember("A", "b", resolverMember!),
                resolverType: type);

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("StringProp", result);
        }

        [Fact]
        public async Task Compile_Arguments_CancellationToken()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithCancellationToken));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupGet(t => t.RequestAborted).Returns(CancellationToken.None);
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("cancel", result);
        }

        [Fact]
        public async Task Compile_Arguments_ResolverContext()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithResolverContext));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Obsolete]
        [Fact]
        public async Task Compile_Arguments_FieldSelection()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember = type.GetMethod(nameof(Resolvers.ResolverWithFieldSelection));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
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
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_ObjectType()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithObjectType));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupGet(t => t.ObjectType).Returns(queryType);
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_Operation()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithOperationDefinition));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
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
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_ObjectField()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithObjectField));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupGet(t => t.Field).Returns(queryType.Fields.First());
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_IOutputField()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithOutputField));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create();

            ObjectType queryType = schema.GetType<ObjectType>("Query");
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupGet(t => t.Field).Returns(queryType.Fields.First());
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_Document()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithDocument));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.SetupGet(t => t.Document)
                .Returns(new DocumentNode(
                    null,
                    Array.Empty<IDefinitionNode>()));
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_Schema()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithSchema));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create();

            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupGet(t => t.Schema).Returns(schema);
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_Service()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolverWithService));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.Service<MyService>()).Returns(new MyService());
            var result = (bool)(await resolver.Resolver(context.Object))!;
            Assert.True(result);
        }

        [Fact]
        public async Task Compile_Arguments_ContextData()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolveWithContextData));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>
            {
                { "foo", "bar"}
            };

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("bar", result);
        }

        [Fact]
        public async Task Compile_Arguments_ContextData_DefaultValue()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolveWithContextDataDefault));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>();

            // act
            var compiler = new ResolveCompiler();
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
                type.GetMethod(nameof(Resolvers.ResolveWithContextData));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>();

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            void Action() => resolver.Resolver(context.Object);
            Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public async Task Compile_Arguments_ScopedContextData()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolveWithScopedContextData));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty.SetItem("foo", "bar");

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ScopedContextData).Returns(contextData);
            var result = (string)await resolver.Resolver(context.Object);
            Assert.Equal("bar", result);
        }

        [Fact]
        public async Task Compile_Arguments_ScopedContextData_DefaultValue()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.ResolveWithScopedContextDataDefault));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            var compiler = new ResolveCompiler();
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
                type.GetMethod(nameof(Resolvers.ResolveWithScopedContextData));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ScopedContextData).Returns(contextData);
            void Action() => resolver.Resolver(context.Object);
            Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public async Task Compile_GetGlobalState_With_Key()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetGlobalStateWithKey));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>
            {
                { "foo", "bar" }
            };

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("bar", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetGlobalState()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetGlobalState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>
            {
                { "foo", "bar" }
            };

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("bar", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetGlobalState_State_Does_Not_Exist()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetGlobalState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await resolver.Resolver(context.Object));
        }

        [Fact]
        public async Task Compile_GetGlobalState_With_Default_Abc()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetGlobalStateWithDefaultAbc));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("abc", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetGlobalState_With_Default()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetGlobalStateWithDefault));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Null(value);
        }

        [Fact]
        public async Task Compile_GetGlobalState_Nullable()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetGlobalStateNullable));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Null(value);
        }

        [Fact]
        public async Task Compile_SetGlobalState()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.SetGlobalState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            await resolver.Resolver(context.Object);

            Assert.True(contextData.ContainsKey("foo"));
            Assert.Equal("abc", contextData["foo"]);
        }

        [Fact]
        public async Task Compile_SetGlobalState_Generic()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.SetGlobalStateGeneric));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            await resolver.Resolver(context.Object);

            Assert.True(contextData.ContainsKey("foo"));
            Assert.Equal("abc", contextData["foo"]);
        }

        [Fact]
        public async Task Compile_GetScopedState_With_Key()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetScopedStateWithKey));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>
            {
                { "foo", "bar" }
            }.ToImmutableDictionary();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("bar", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetScopedState()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetScopedState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>
            {
                { "foo", "bar" }
            }.ToImmutableDictionary();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("bar", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetScopedState_State_Does_Not_Exist()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetScopedState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await resolver.Resolver(context.Object));
        }

        [Fact]
        public async Task Compile_GetScopedState_With_Default_Abc()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetScopedStateWithDefaultAbc));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("abc", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetScopedState_With_Default()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetScopedStateWithDefault));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Null(value);
        }

        [Fact]
        public async Task Compile_GetScopedState_Nullable()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetScopedStateNullable));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Null(value);
        }

        [Fact]
        public async Task Compile_SetScopedState()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.SetScopedState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            IResolverContext resolverContext = context.Object;

            await resolver.Resolver(resolverContext);

            Assert.True(resolverContext.ScopedContextData.ContainsKey("foo"));
            Assert.Equal("abc", resolverContext.ScopedContextData["foo"]);
        }

        [Fact]
        public async Task Compile_SetScopedState_Generic()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.SetScopedStateGeneric));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            IResolverContext resolverContext = context.Object;

            await resolver.Resolver(resolverContext);

            Assert.True(resolverContext.ScopedContextData.ContainsKey("foo"));
            Assert.Equal("abc", resolverContext.ScopedContextData["foo"]);
        }

        [Fact]
        public async Task Compile_GetLocalState_With_Key()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetLocalStateWithKey));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>
            {
                { "foo", "bar" }
            }.ToImmutableDictionary();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.LocalContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("bar", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetLocalState()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetLocalState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = new Dictionary<string, object>
            {
                { "foo", "bar" }
            }.ToImmutableDictionary();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.LocalContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("bar", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetLocalState_State_Does_Not_Exist()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetLocalState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.LocalContextData, contextData);
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await resolver.Resolver(context.Object));
        }

        [Fact]
        public async Task Compile_GetLocalState_With_Default_Abc()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetLocalStateWithDefaultAbc));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.LocalContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Equal("abc", Assert.IsType<string>(value));
        }

        [Fact]
        public async Task Compile_GetLocalState_With_Default()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.GetLocalStateWithDefault));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            var contextData = ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.LocalContextData, contextData);
            object value = await resolver.Resolver(context.Object);
            Assert.Null(value);
        }

        [Fact]
        public async Task Compile_SetLocalState()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.SetLocalState));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.LocalContextData, contextData);
            IResolverContext resolverContext = context.Object;

            await resolver.Resolver(resolverContext);

            Assert.True(resolverContext.LocalContextData.ContainsKey("foo"));
            Assert.Equal("abc", resolverContext.LocalContextData["foo"]);
        }

        [Fact]
        public async Task Compile_SetLocalState_Generic()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod(nameof(Resolvers.SetLocalStateGeneric));
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember!));
            ImmutableDictionary<string, object> contextData =
                ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.LocalContextData, contextData);
            IResolverContext resolverContext = context.Object;

            await resolver.Resolver(resolverContext);

            Assert.True(resolverContext.LocalContextData.ContainsKey("foo"));
            Assert.Equal("abc", resolverContext.LocalContextData["foo"]);
        }

        [Fact]
        public async Task SchemaIntegrationTest()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Resolvers>()
                .ModifyOptions(o => o.SortFieldsByName = true)
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        public class Resolvers
        {
            public Task<object> ObjectTaskResolver() =>
                Task.FromResult<object>("ObjectResolverResult");

            public Task<string> StringTaskResolver() =>
                Task.FromResult("StringTaskResolver");

            public Task<string> StringTaskResolverWithArg(string a) =>
                Task.FromResult(a);

            public object ObjectResolver() => "ObjectResolverResult";

            public string StringResolver() => "StringTaskResolver";

            public string StringResolverWithArg(string a) => a;

            [GraphQLIgnore]
            public string StringValueNodeResolverWithArg(StringValueNode a) => a.Value;

            public string OptionalStringResolverWithArg(Optional<string> a) => a.Value;

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

#pragma warning disable CS0618
            public string ResolveWithContextData(
                [State("foo")]string s) => s;

            public string ResolveWithContextDataDefault(
                [State("foo", DefaultIfNotExists = true)]string s) => s;

            public string ResolveWithScopedContextData(
                [State("foo", IsScoped = true)]string s) => s;

            public string ResolveWithScopedContextDataDefault(
                [State("foo", IsScoped = true, DefaultIfNotExists = true)]
                string s) => s;
#pragma warning restore CS0618

            public string GetGlobalStateWithKey(
                [GlobalState("foo")]
                string s) => s;

            public string GetGlobalState(
                [GlobalState]
                string foo) => foo;

            public string GetGlobalStateWithDefaultAbc(
                [GlobalState]
                string foo = "abc") => foo;

            public string GetGlobalStateWithDefault(
                [GlobalState]
                string foo = default) => foo;

            #nullable  enable
            public string GetGlobalStateNullable(
                [GlobalState]
                string? foo) => foo!;
            #nullable  disable

            public string SetGlobalStateGeneric(
                [GlobalState]
                SetState<string> foo)
            {
                foo("abc");
                return "foo";
            }

            public string SetGlobalState(
                [GlobalState]
                SetState foo)
            {
                foo("abc");
                return "foo";
            }

            public string GetScopedStateWithKey(
               [ScopedState("foo")]
                string s) => s;

            public string GetScopedState(
                [ScopedState]
                string foo) => foo;

            public string GetScopedStateWithDefaultAbc(
                [ScopedState]
                string foo = "abc") => foo;

            public string GetScopedStateWithDefault(
                [ScopedState]
                string foo = default) => foo;

            #nullable  enable
            public string GetScopedStateNullable(
                [ScopedState]
                string? foo) => foo!;
            #nullable disable

            public string SetScopedStateGeneric(
                [ScopedState]
                SetState<string> foo)
            {
                foo("abc");
                return "foo";
            }

            public string SetScopedState(
                [ScopedState]
                SetState foo)
            {
                foo("abc");
                return "foo";
            }

            public string GetLocalStateWithKey(
                [LocalState("foo")]
                string s) => s;

            public string GetLocalState(
                [LocalState]
                string foo) => foo;

            public string GetLocalStateWithDefaultAbc(
                [LocalState]
                string foo = "abc") => foo;

            public string GetLocalStateWithDefault(
                [LocalState]
                string foo = default) => foo;

            public string SetLocalStateGeneric(
                [LocalState]
                SetState<string> foo)
            {
                foo("abc");
                return "foo";
            }

            public string SetLocalState(
                [LocalState]
                SetState foo)
            {
                foo("abc");
                return "foo";
            }
        }

        public class Entity { }

        public class MyService { }
    }
}
