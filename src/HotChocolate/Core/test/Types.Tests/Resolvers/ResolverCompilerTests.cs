using System.Collections.Immutable;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Moq;

#nullable enable

namespace HotChocolate.Resolvers;

public class ResolverCompilerTests
{
    private readonly IParameterExpressionBuilder[] _empty = [];

    [Fact]
    public async Task Compile_TaskObjMethod_NoParams_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ObjectTaskResolver))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("ObjectResolverResult", result);
    }

    [Fact]
    public async Task Compile_TaskStringMethod_NoParams_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringTaskResolver))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("StringTaskResolver", result);
    }

    [Fact]
    public async Task Compile_TaskStringMethod_WithParams_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringTaskResolverWithArg))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ArgumentValue<string>("a")).Returns("abc");
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("abc", result);
    }

    [Fact]
    public async Task Compile_ObjMethod_NoParams_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ObjectResolver))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("ObjectResolverResult", result);
    }

    [Fact]
    public async Task Compile_StringMethod_NoParams_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringResolver))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("StringTaskResolver", result);
    }

    [Fact]
    public async Task Compile_StringMethod_WithParams_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringResolverWithArg))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ArgumentValue<string>("a")).Returns("abc");
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("abc", result);
    }

    [Fact]
    public async Task Compile_StringValueNodeMethod_WithParams_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringValueNodeResolverWithArg))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ArgumentLiteral<StringValueNode>("a"))
            .Returns(new StringValueNode("abc"));
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("abc", result);
    }

    [Fact]
    public async Task Compile_OptionalStringMethod_WithParams_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.OptionalStringResolverWithArg))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ArgumentOptional<string>("a"))
            .Returns(new Optional<string>("abc"));
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("abc", result);
    }

    [Fact]
    public async Task Compile_ObjTaskProperty_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetProperty("ObjectTaskStringProp")!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("ObjectTaskStringProp", result);
    }

    [Fact]
    public async Task Compile_StringTaskProperty_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetProperty("StringTaskResolverProp")!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("StringTaskResolverProp", result);
    }

    [Fact]
    public async Task Compile_StringProperty_SourceResolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetProperty("StringProp")!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("StringProp", result);
    }

    [Fact]
    public async Task Compile_TaskObjMethod_NoParams_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ObjectTaskResolver))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("ObjectResolverResult", result);
    }

    [Fact]
    public async Task Compile_TaskStringMethod_NoParams_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringTaskResolver))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("StringTaskResolver", result);
    }

    [Fact]
    public async Task Compile_TaskStringMethod_WithParams_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringTaskResolverWithArg))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ArgumentValue<string>("a")).Returns("abc");
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("abc", result);
    }

    [Fact]
    public async Task Compile_ObjMethod_NoParams_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ObjectResolver))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("ObjectResolverResult", result);
    }

    [Fact]
    public async Task Compile_StringMethod_NoParams_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringResolver))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("StringTaskResolver", result);
    }

    [Fact]
    public async Task Compile_StringMethod_WithParams_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.StringResolverWithArg))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ArgumentValue<string>("a")).Returns("abc");
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("abc", result);
    }

    [Fact]
    public async Task Compile_ObjTaskProperty_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetProperty("ObjectTaskStringProp")!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("ObjectTaskStringProp", result);
    }

    [Fact]
    public async Task Compile_StringTaskProperty_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetProperty("StringTaskResolverProp")!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("StringTaskResolverProp", result);
    }

    [Fact]
    public async Task Compile_StringProperty_Resolver()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetProperty("StringProp")!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver =
            compiler.CompileResolve(member, typeof(Entity), type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Resolver<Resolvers>()).Returns(new Resolvers());
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("StringProp", result);
    }

    [Fact]
    public async Task Compile_Arguments_CancellationToken()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithCancellationToken))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.RequestAborted).Returns(CancellationToken.None);
        var result = (string)(await resolver(context.Object))!;
        Assert.Equal("cancel", result);
    }

    [Fact]
    public async Task Compile_Arguments_ResolverContext()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithResolverContext))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_Arguments_FieldSelection()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithFieldSelection))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var selection = new Mock<ISelection>();

        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Selection).Returns(selection.Object);

        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_Arguments_Selection()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithSelection))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;
        var pure = compiler.CompileResolve(member, type).PureResolver!;

        // assert
        var selection = new Mock<ISelection>();

        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Selection).Returns(selection.Object);

        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result, "Standard Resolver");

        result = (bool)pure(context.Object)!;
        Assert.True(result, "Pure Resolver");
    }

    [Fact]
    public async Task Compile_Arguments_FieldSyntax()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithFieldSyntax))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;
        var pure = compiler.CompileResolve(member, type).PureResolver!;

        // assert
        var fieldSyntax = new FieldNode(
            null,
            new NameNode("foo"),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            null);

        var selection = new Mock<ISelection>();
        selection.SetupGet(t => t.SyntaxNode).Returns(fieldSyntax);

        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Selection).Returns(selection.Object);

        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result, "Standard Resolver");

        result = (bool)pure(context.Object)!;
        Assert.True(result, "Pure Resolver");
    }

    [Fact]
    public async Task Compile_Arguments_ObjectType()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithObjectType))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { a: String }")
            .Use(next => next)
            .Create();

        var queryType = schema.GetType<ObjectType>("Query");
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.ObjectType).Returns(queryType);
        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_Arguments_Operation()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithOperationDefinition))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var operationDefinition =
            new OperationDefinitionNode(
                null,
                null,
                OperationType.Query,
                Array.Empty<VariableDefinitionNode>(),
                Array.Empty<DirectiveNode>(),
                new SelectionSetNode(
                    null,
                    Array.Empty<ISelectionNode>()));

        var operation = new Mock<IOperation>();
        operation.Setup(t => t.Definition).Returns(operationDefinition);

        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Operation).Returns(operation.Object);
        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_Arguments_ObjectField()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithObjectField))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { a: String }")
            .Use(next => next)
            .Create();

        var queryType = schema.GetType<ObjectType>("Query");

        var selection = new Mock<ISelection>();
        selection.SetupGet(t => t.Field).Returns(queryType.Fields.First());

        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Selection).Returns(selection.Object);

        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_Arguments_IOutputField()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithOutputField))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { a: String }")
            .Use(next => next)
            .Create();

        var queryType = schema.GetType<ObjectType>("Query");

        var selection = new Mock<ISelection>();
        selection.SetupGet(t => t.Field).Returns(queryType.Fields.First());

        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Selection).Returns(selection.Object);

        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_Arguments_Document()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithDocument))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var document = new DocumentNode(Array.Empty<IDefinitionNode>());
        var operation = new Mock<IOperation>();
        operation.Setup(t => t.Document).Returns(document);

        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Operation).Returns(operation.Object);
        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_Arguments_Schema()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithSchema))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { a: String }")
            .Use(next => next)
            .Create();

        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Schema).Returns(schema);
        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_Arguments_Service()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.ResolverWithService))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(t => t.GetService(typeof(MyService))).Returns(new MyService());
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.Services).Returns(serviceProvider.Object);
        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
    }

    [Fact]
    public async Task Compile_GetGlobalState_With_Key()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetGlobalStateWithKey))!;
        var contextData = new Dictionary<string, object?> { { "foo", "bar" }, };

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        var value = await resolver(context.Object);
        Assert.Equal("bar", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetGlobalState()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetGlobalState))!;
        var contextData = new Dictionary<string, object?> { { "foo", "bar" }, };

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        var value = await resolver(context.Object);
        Assert.Equal("bar", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetGlobalState_State_Does_Not_Exist()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetGlobalState))!;
        var contextData = new Dictionary<string, object?>();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await resolver(context.Object));
    }

    [Fact]
    public async Task Compile_GetGlobalState_With_Default_Abc()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetGlobalStateWithDefaultAbc))!;
        var contextData = new Dictionary<string, object?>();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        var value = await resolver(context.Object);
        Assert.Equal("abc", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetGlobalState_With_Default()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member =  type.GetMethod(nameof(Resolvers.GetGlobalStateWithDefault))!;
        var contextData = new Dictionary<string, object?>();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        var value = await resolver(context.Object);
        Assert.Null(value);
    }

    [Fact]
    public async Task Compile_GetGlobalState_Nullable()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetGlobalStateNullable))!;
        var contextData = new Dictionary<string, object?>();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        var value = await resolver(context.Object);
        Assert.Null(value);
    }

    [Fact]
    public async Task Compile_SetGlobalState()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.SetGlobalState))!;
        var contextData = new Dictionary<string, object?>();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        await resolver(context.Object);

        Assert.True(contextData.ContainsKey("foo"));
        Assert.Equal("abc", contextData["foo"]);
    }

    [Fact]
    public async Task Compile_SetGlobalState_Generic()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.SetGlobalStateGeneric))!;

        var contextData = new Dictionary<string, object?>();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        await resolver(context.Object);

        Assert.True(contextData.ContainsKey("foo"));
        Assert.Equal("abc", contextData["foo"]);
    }

    [Fact]
    public async Task Compile_GetScopedState_With_Key()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetScopedStateWithKey))!;
        var contextData = new Dictionary<string, object?> { { "foo", "bar" }, }
            .ToImmutableDictionary();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.ScopedContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Equal("bar", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetScopedState()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetScopedState))!;

        var contextData = new Dictionary<string, object?>{ { "foo", "bar" }, }
            .ToImmutableDictionary();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.ScopedContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Equal("bar", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetScopedState_State_Does_Not_Exist()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetScopedState))!;
        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.ScopedContextData, contextData);
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await resolver(context.Object));
    }

    [Fact]
    public async Task Compile_GetScopedState_With_Default_Abc()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetScopedStateWithDefaultAbc))!;

        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.ScopedContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Equal("abc", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetScopedState_With_Default()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetScopedStateWithDefault))!;

        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.ScopedContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Null(value);
    }

    [Fact]
    public async Task Compile_GetScopedState_Nullable()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetScopedStateNullable))!;

        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(() => new Resolvers());
        context.SetupProperty(t => t.ScopedContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Null(value);
    }

    [Fact]
    public async Task Compile_SetScopedState()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.SetScopedState))!;

        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.ScopedContextData, contextData);
        var resolverContext = context.Object;

        await resolver(resolverContext);

        Assert.True(resolverContext.ScopedContextData.ContainsKey("foo"));
        Assert.Equal("abc", resolverContext.ScopedContextData["foo"]);
    }

    [Fact]
    public async Task Compile_SetScopedState_Generic()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.SetScopedStateGeneric))!;

        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.ScopedContextData, contextData);
        var resolverContext = context.Object;

        await resolver(resolverContext);

        Assert.True(resolverContext.ScopedContextData.ContainsKey("foo"));
        Assert.Equal("abc", resolverContext.ScopedContextData["foo"]);
    }

    [Fact]
    public async Task Compile_GetLocalState_With_Key()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetLocalStateWithKey))!;
        var contextData = new Dictionary<string, object?> { { "foo", "bar" }, }
            .ToImmutableDictionary();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.LocalContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Equal("bar", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetLocalState()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetLocalState))!;
        var contextData = new Dictionary<string, object?> { { "foo", "bar" }, }
            .ToImmutableDictionary();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.LocalContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Equal("bar", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetLocalState_State_Does_Not_Exist()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetLocalState))!;
        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.LocalContextData, contextData);
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await resolver(context.Object));
    }

    [Fact]
    public async Task Compile_GetLocalState_With_Default_Abc()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetLocalStateWithDefaultAbc))!;
        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.LocalContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Equal("abc", Assert.IsType<string>(value));
    }

    [Fact]
    public async Task Compile_GetLocalState_With_Default()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetLocalStateWithDefault))!;
        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.LocalContextData, contextData);
        var value = await resolver(context.Object);
        Assert.Null(value);
    }

    [Fact]
    public async Task Compile_SetLocalState()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.SetLocalState))!;

        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.LocalContextData, contextData);
        var resolverContext = context.Object;

        await resolver(resolverContext);

        Assert.True(resolverContext.LocalContextData.ContainsKey("foo"));
        Assert.Equal("abc", resolverContext.LocalContextData["foo"]);
    }

    [Fact]
    public async Task Compile_SetLocalState_Generic()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.SetLocalStateGeneric))!;

        var contextData =
            ImmutableDictionary<string, object?>.Empty;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupProperty(t => t.LocalContextData, contextData);
        var resolverContext = context.Object;

        await resolver(resolverContext);

        Assert.True(resolverContext.LocalContextData.ContainsKey("foo"));
        Assert.Equal("abc", resolverContext.LocalContextData["foo"]);
    }

    [Fact]
    public async Task Compile_GetClaimsPrincipal()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetClaimsPrincipal))!;

        var contextData = new Dictionary<string, object?>
        {
            { nameof(ClaimsPrincipal), new ClaimsPrincipal() },
        };

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        var value = await resolver(context.Object);
        Assert.True(Assert.IsType<bool>(value));
    }

    [Fact]
    public async Task Compile_GetClaimsPrincipal_ClaimsNotExists()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetClaimsPrincipal))!;

        var contextData = new Dictionary<string, object?>();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        async Task Execute() => await resolver(context.Object);
        Exception ex = await Assert.ThrowsAsync<ArgumentException>(Execute);
        ex.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Compile_GetNullableClaimsPrincipal_ClaimsNotExists()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetNullableClaimsPrincipal))!;

        var contextData = new Dictionary<string, object?>();

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.Setup(t => t.ContextData).Returns(contextData);
        async Task Execute() => await resolver(context.Object);
        Exception ex = await Assert.ThrowsAsync<ArgumentException>(Execute);
        ex.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Compile_Arguments_Path()
    {
        // arrange
        var type = typeof(Resolvers);
        MemberInfo member = type.GetMethod(nameof(Resolvers.GetPath))!;

        // act
        var compiler = new DefaultResolverCompiler(EmptyServiceProvider.Instance, _empty);
        var resolver = compiler.CompileResolve(member, type).Resolver!;

        // assert
        var context = new Mock<IResolverContext>();
        context.Setup(t => t.Parent<Resolvers>()).Returns(new Resolvers());
        context.SetupGet(t => t.Path).Returns(Path.Root.Append("FOO"));

        var result = (bool)(await resolver(context.Object))!;
        Assert.True(result);
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

    [Fact]
    public async Task ScopedExpressionBuilderTest()
    {
        // arrange
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithScopedExpressionBuilder>()
            .ModifyOptions(o => o.SortFieldsByName = true)
            .ExecuteRequestAsync("{ bar }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ScopedExpressionBuilderSchemaTest()
    {
        // arrange
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithScopedExpressionBuilder>()
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

        public string? OptionalStringResolverWithArg(Optional<string> a) => a.Value;

        public Task<object> ObjectTaskStringProp { get; } =
            Task.FromResult<object>("ObjectTaskStringProp");

        public Task<string> StringTaskResolverProp { get; } =
            Task.FromResult("StringTaskResolverProp");

        public string StringProp { get; } = "StringProp";

        public string ResolverWithCancellationToken(
            CancellationToken cancellationToken) =>
            "cancel";

        public bool GetClaimsPrincipal(ClaimsPrincipal claims)
            => claims != null!;

        public bool GetNullableClaimsPrincipal(ClaimsPrincipal? claims)
            => claims != null!;

        public bool GetPath(Path path)
            => path != null!;

        public bool ResolverWithResolverContext(
            IResolverContext context) =>
            context != null!;

        public bool ResolverWithFieldSyntax(
            FieldNode fieldSyntax) =>
            fieldSyntax != null!;

        public bool ResolverWithFieldSelection(
            ISelection fieldSelection) =>
            fieldSelection != null!;

        public bool ResolverWithSelection(
            ISelection fieldSelection) =>
            fieldSelection != null!;

        public bool ResolverWithObjectType(
            ObjectType objectType) =>
            objectType != null!;

        public bool ResolverWithOperationDefinition(
            OperationDefinitionNode operationDefinition) =>
            operationDefinition != null!;

        public bool ResolverWithObjectField(
            ObjectField objectField) =>
            objectField != null!;

        public bool ResolverWithOutputField(
            IOutputField outputField) =>
            outputField != null!;

        public bool ResolverWithDocument(
            DocumentNode document) =>
            document != null!;

        public bool ResolverWithSchema(
            ISchema schema) =>
            schema != null!;

        public bool ResolverWithService(
            [Service] MyService service) =>
            service != null!;

        public string GetGlobalStateWithKey(
            [GlobalState("foo")]
            string s) => s;

        public string GetGlobalState(
            [GlobalState]
            string foo) => foo;

        public string GetGlobalStateWithDefaultAbc(
            [GlobalState]
            string foo = "abc") => foo;

        public string? GetGlobalStateWithDefault(
            [GlobalState]
            string? foo = default) => foo;

        public string GetGlobalStateNullable(
            [GlobalState]
            string? foo) => foo!;

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

        public string? GetScopedStateWithDefault(
            [ScopedState]
            string? foo = default) => foo;

        public string GetScopedStateNullable(
            [ScopedState]
            string? foo) => foo!;

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

        public string? GetLocalStateWithDefault(
            [LocalState]
            string? foo = default) => foo;

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

    public class Entity;

    public class MyService;

    public class QueryWithScopedExpressionBuilder
    {
        [UseSomeState]
        public string Bar(SomeState someState)
            => someState.Foo;
    }

    public class SomeState
    {
        public string Foo => "Abc";
    }

    public class UseSomeStateAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Use(
                n => c =>
                {
                    c.SetLocalState("foo", new SomeState());
                    return n(c);
                });

            descriptor.Extend().Definition.ParameterExpressionBuilders.Add(
                new CustomParameterExpressionBuilder<SomeState>(
                    t => t.GetLocalState<SomeState>("foo")!));
        }
    }
}
