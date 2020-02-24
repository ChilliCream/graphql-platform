﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
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
            MemberInfo resolverMember = type.GetMethod("StringTaskResolver");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

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
                type.GetMethod("StringTaskResolverWithArg");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));

            // act
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>())
                .Returns(new Resolvers());
            context.Setup(t => t.CustomProperty<IEventMessage>(
                WellKnownContextData.EventMessage))
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
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create();
            ;
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
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create();
            ;
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
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create();
            ;
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
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { a: String }")
                .AddResolver("Query", "a", "foo")
                .Create();
            ;
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
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
                type.GetMethod("ResolveWithContextData");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = new Dictionary<string, object>();

            // act
            var compiler = new ResolveCompiler();
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
            var compiler = new ResolveCompiler();
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
                type.GetMethod("ResolveWithScopedContextData");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty;

            // act
            var compiler = new ResolveCompiler();
            FieldResolver resolver = compiler.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ScopedContextData).Returns(contextData);
            Action action = () => resolver.Resolver(context.Object);
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public async Task Compile_GetGlobalState_With_Key()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("GetGlobalStateWithKey");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
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
                type.GetMethod("GetGlobalState");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
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
                type.GetMethod("GetGlobalState");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = new Dictionary<string, object>();

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.Setup(t => t.ContextData).Returns(contextData);
            await Assert.ThrowsAsync<ArgumentException>(() => resolver.Resolver(context.Object));
        }

        [Fact]
        public async Task Compile_GetGlobalState_With_Default_Abc()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("GetGlobalStateWithDefaultAbc");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
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
                type.GetMethod("GetGlobalStateWithDefault");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
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
                type.GetMethod("SetGlobalState");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
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
                type.GetMethod("SetGlobalStateGeneric");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
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
                type.GetMethod("GetScopedStateWithKey");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
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
                type.GetMethod("GetScopedState");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
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
                type.GetMethod("GetScopedState");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty;

            // act
            FieldResolver resolver = ResolverCompiler.Resolve.Compile(resolverDescriptor);

            // assert
            var context = new Mock<IResolverContext>();
            context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
            context.SetupProperty(t => t.ScopedContextData, contextData);
            await Assert.ThrowsAsync<ArgumentException>(() => resolver.Resolver(context.Object));
        }

        [Fact]
        public async Task Compile_GetScopedState_With_Default_Abc()
        {
            // arrange
            Type type = typeof(Resolvers);
            MemberInfo resolverMember =
                type.GetMethod("GetScopedStateWithDefaultAbc");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty;

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
                type.GetMethod("GetScopedStateWithDefault");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty;

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
                type.GetMethod("SetScopedState");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty;

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
                type.GetMethod("SetScopedStateGeneric");
            var resolverDescriptor = new ResolverDescriptor(
                type,
                new FieldMember("A", "b", resolverMember));
            var contextData = ImmutableDictionary<string, object>.Empty;

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
        }

        public class Entity { }

        public class MyService { }
    }
}
