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

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterVisitorMethodTests
        : FilterVisitorTestBase
    {
        [Fact]
        public void Create_MethodSimple_Expression()
        {
            // arrange
            IValueNode? value = Syntax.ParseValueLiteral("{ simple: { eq:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(
                new FooFilterType(),
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
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "a" };
            Assert.True(func(a));

            var b = new Foo { Bar = "b" };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_MethodComplex_Expression()
        {
            // arrange
            ExecutorBuilder? tester = CreateProviderTester(
                new FooFilterType(),
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

            IValueNode? valueTrue = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ complex: {parameter:\"a\", eq:\"a\" }}");

            IValueNode? valueFalse = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ complex: {parameter:\"a\", eq:\"b\" }}");
            // act
            Func<Foo, bool>? funcTrue = tester.Build<Foo>(valueTrue);
            Func<Foo, bool>? funcFalse = tester.Build<Foo>(valueFalse);

            // assert
            var a = new Foo();
            Assert.True(funcTrue(a));

            var b = new Foo();
            Assert.False(funcFalse(b));
        }

        private class QueryableSimpleMethodTest
            : QueryableDefaultFieldHandler
        {
            private static readonly MethodInfo Method = typeof(Foo).GetMethod(nameof(Foo.Simple))!;
            private IExtendedType _extendedType;

            public QueryableSimpleMethodTest(ITypeInspector typeInspector)
            {
                _extendedType = typeInspector.GetReturnType(Method);
            }

            public override bool CanHandle(
                ITypeDiscoveryContext context,
                FilterInputTypeDefinition typeDefinition,
                FilterFieldDefinition fieldDefinition)
            {
                return fieldDefinition is FilterOperationFieldDefinition { Id: 155 };
            }

            public override bool TryHandleEnter(
                QueryableFilterContext context,
                IFilterField field,
                ObjectFieldNode node,
                out ISyntaxVisitorAction? action)
            {
                if (node.Value.IsNull())
                {
                    context.ReportError(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The provided value for filter `{0}` of type {1} is invalid. " +
                                "Null values are not supported.",
                                context.Operations.Peek().Name,
                                field.Type.Visualize())
                            .Build());

                    action = SyntaxVisitor.Skip;
                    return true;
                }

                Expression nestedProperty = Expression.Call(context.GetInstance(), Method);

                context.PushInstance(nestedProperty);
                context.RuntimeTypes.Push(_extendedType!);
                action = SyntaxVisitor.Continue;
                return true;
            }
        }

        private class QueryableComplexMethodTest
            : QueryableDefaultFieldHandler
        {
            private static readonly MethodInfo Method = typeof(Foo).GetMethod(nameof(Foo.Complex))!;

            private IExtendedType? _extendedType;

            public override bool CanHandle(
                ITypeDiscoveryContext context,
                FilterInputTypeDefinition typeDefinition,
                FilterFieldDefinition fieldDefinition)
            {
                _extendedType ??= context.TypeInspector.GetReturnType(Method);

                return fieldDefinition is FilterOperationFieldDefinition { Id: 156 };
            }

            public override bool TryHandleEnter(
                QueryableFilterContext context,
                IFilterField field,
                ObjectFieldNode node,
                out ISyntaxVisitorAction? action)
            {
                if (node.Value.IsNull())
                {
                    context.ReportError(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The provided value for filter `{0}` of type {1} is invalid. " +
                                "Null values are not supported.",
                                context.Operations.Peek().Name,
                                field.Type.Visualize())
                            .Build());

                    action = SyntaxVisitor.Skip;
                    return true;
                }

                if (field.Type is StringOperationFilterInput operationType &&
                    node.Value is ObjectValueNode objectValue)
                {
                    IValueNode? parameterNode = null;

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

                    object? value =
                        operationType.Fields["parameter"].Type.ParseLiteral(parameterNode);

                    Expression nestedProperty = Expression.Call(
                        context.GetInstance(),
                        Method,
                        Expression.Constant(value));

                    context.PushInstance(nestedProperty);
                    context.RuntimeTypes.Push(_extendedType!);
                    action = SyntaxVisitor.Continue;
                    return true;
                }

                action = null;
                return false;
            }
        }

        public class Foo
        {
            public string Bar { get; set; }

            public string Simple() => Bar;

            public string Complex(string parameter) => parameter;
        }

        private class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar).Ignore();
                descriptor.Operation(156).Type<TestComplexFilterInput>();
                descriptor.Operation(155).Type<StringOperationFilterInput>();
            }
        }

        private class TestComplexFilterInput : StringOperationFilterInput
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                base.Configure(descriptor);

                descriptor.Operation(DefaultOperations.Data)
                    .Name("parameter")
                    .Type<NonNullType<StringType>>();
            }
        }
    }
}
