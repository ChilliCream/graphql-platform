using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorMethodTests : FilterVisitorTestBase
{
    [Fact]
    public void Create_MethodSimple_Expression()
    {
        // arrange
        var value = Syntax.ParseValueLiteral("{ simple: { eq:\"a\" }}");
        var tester = CreateProviderTester(
            new FooFilterInput(),
            new FilterConvention(
                x =>
                {
                    x.Operation(155).Name("simple");
                    x.Operation(156).Name("complex");
                    x.AddDefaults();
                    x.Provider(
                        new QueryableFilterProvider(
                            p => p.AddFieldHandler<QueryableSimpleMethodTest>()
                                .AddFieldHandler<QueryableComplexMethodTest>()
                                .AddDefaultFieldHandlers()));
                }));

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.True(func(a));

        var b = new Foo { Bar = "b", };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_MethodComplex_Expression()
    {
        // arrange
        var tester = CreateProviderTester(
            new FooFilterInput(),
            new FilterConvention(
                x =>
                {
                    x.Operation(155).Name("simple");
                    x.Operation(156).Name("complex");
                    x.AddDefaults();
                    x.Provider(
                        new QueryableFilterProvider(
                            p => p.AddFieldHandler<QueryableSimpleMethodTest>()
                                .AddFieldHandler<QueryableComplexMethodTest>()
                                .AddDefaultFieldHandlers()));
                }));

        var valueTrue = Syntax.ParseValueLiteral(
            "{ complex: {parameter:\"a\", eq:\"a\" }}");

        var valueFalse = Syntax.ParseValueLiteral(
            "{ complex: {parameter:\"a\", eq:\"b\" }}");
        // act
        var funcTrue = tester.Build<Foo>(valueTrue);
        var funcFalse = tester.Build<Foo>(valueFalse);

        // assert
        var a = new Foo();
        Assert.True(funcTrue(a));

        var b = new Foo();
        Assert.False(funcFalse(b));
    }

    private sealed class QueryableSimpleMethodTest : QueryableDefaultFieldHandler
    {
        private static readonly MethodInfo _method = typeof(Foo).GetMethod(nameof(Foo.Simple))!;
        private readonly IExtendedType _extendedType;

        public QueryableSimpleMethodTest(ITypeInspector typeInspector)
        {
            _extendedType = typeInspector.GetReturnType(_method);
        }

        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition { Id: 155, };
        }

        public override bool TryHandleEnter(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            out ISyntaxVisitorAction action)
        {
            if (node.Value.IsNull())
            {
                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage(
                            "The provided value for filter `{0}` of type {1} is invalid. " +
                            "Null values are not supported.",
                            context.Operations.Peek().Name,
                            field.Type.Print())
                        .Build());

                action = SyntaxVisitor.Skip;
                return true;
            }

            Expression nestedProperty = Expression.Call(context.GetInstance(), _method);

            context.PushInstance(nestedProperty);
            context.RuntimeTypes.Push(_extendedType!);
            action = SyntaxVisitor.Continue;
            return true;
        }
    }

    private sealed class QueryableComplexMethodTest : QueryableDefaultFieldHandler
    {
        private static readonly MethodInfo _method =
            typeof(Foo).GetMethod(nameof(Foo.Complex))!;

        private IExtendedType _extendedType = null!;
        private readonly InputParser _inputParser;

        public QueryableComplexMethodTest(InputParser inputParser)
        {
            _inputParser = inputParser;
        }

        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            _extendedType ??= context.TypeInspector.GetReturnType(_method);
            return fieldDefinition is FilterOperationFieldDefinition { Id: 156, };
        }

        public override bool TryHandleEnter(
            QueryableFilterContext context,
            IFilterField field,
            ObjectFieldNode node,
            out ISyntaxVisitorAction action)
        {
            if (node.Value.IsNull())
            {
                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage(
                            "The provided value for filter `{0}` of type {1} is invalid. " +
                            "Null values are not supported.",
                            context.Operations.Peek().Name,
                            field.Type.Print())
                        .Build());

                action = SyntaxVisitor.Skip;
                return true;
            }

            if (field.Type is StringOperationFilterInputType operationType &&
                node.Value is ObjectValueNode objectValue)
            {
                IValueNode parameterNode = null!;

                for (var i = 0; i < objectValue.Fields.Count; i++)
                {
                    if (objectValue.Fields[i].Name.Value == "parameter")
                    {
                        parameterNode = objectValue.Fields[i].Value;
                    }
                }

                if (parameterNode is null)
                {
                    throw new InvalidOperationException();
                }

                var value =
                    _inputParser
                        .ParseLiteral(parameterNode, operationType.Fields["parameter"].Type);

                Expression nestedProperty = Expression.Call(
                    context.GetInstance(),
                    _method,
                    Expression.Constant(value));

                context.PushInstance(nestedProperty);
                context.RuntimeTypes.Push(_extendedType!);
                action = SyntaxVisitor.Continue;
                return true;
            }

            action = null!;
            return false;
        }
    }

    public class Foo
    {
        public string? Bar { get; set; }

        public string Simple() => Bar ?? "Simple";

        public string Complex(string parameter) => parameter;
    }

    private sealed class FooFilterInput
        : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar).Ignore();
            descriptor.Operation(156).Type<TestComplexFilterInputType>();
            descriptor.Operation(155).Type<StringOperationFilterInputType>();
        }
    }

    private sealed class TestComplexFilterInputType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            base.Configure(descriptor);

            descriptor.Operation(DefaultFilterOperations.Data)
                .Name("parameter")
                .Type<NonNullType<StringType>>();
        }
    }
}
