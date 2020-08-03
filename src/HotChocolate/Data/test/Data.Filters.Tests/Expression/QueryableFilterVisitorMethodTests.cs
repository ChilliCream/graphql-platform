using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterVisitorMethodTests
        : FilterVisitorTestBase
    {
        [Fact]
        public void Create_MethodSimple_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ simple: { eq:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType(),
                new FilterConvention(
                    x => x.UseDefault()
                          .Provider(
                            new QueryableFilterProvider(
                                p => p.AddFieldHandler<QueryableComplexMethodTest>()
                                      .UseDefaults()))));

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
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType(),
                new FilterConvention(
                    x => x.UseDefault()
                          .Provider(
                            new QueryableFilterProvider(
                                p => p.AddFieldHandler<QueryableComplexMethodTest>()
                                      .UseDefaults()))));
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

        public class QueryableComplexMethodTest
            : QueryableDefaultFieldHandler
        {
            public static MethodInfo Method = typeof(Foo).GetMethod(nameof(Foo.Complex))!;

            public override bool CanHandle(
                ITypeDiscoveryContext context,
                FilterInputTypeDefinition typeDefinition,
                FilterFieldDefinition fieldDefinition) =>
                !(fieldDefinition is FilterOperationFieldDefinition) &&
                    fieldDefinition.Member == Method;

            public override bool TryHandleEnter(
                QueryableFilterContext context,
                IFilterInputType declaringType,
                IFilterField field,
                IType fieldType,
                ObjectFieldNode node,
                [NotNullWhen(true)] out ISyntaxVisitorAction? action)
            {
                if (node.Value.IsNull())
                {
                    context.ReportError(
                        ErrorHelper.CreateNonNullError(field, node.Value, context));

                    action = SyntaxVisitor.Skip;
                    return true;
                }

                if (fieldType is StringOperationInput operationType &&
                    node.Value is ObjectValueNode objectValue &&
                    field is { })
                {
                    IValueNode? parameterNode = null;

                    for (var i = 0; i < objectValue.Fields.Count; i++)
                    {
                        if (objectValue.Fields[i].Name.Value == "parameter")
                        {
                            parameterNode = objectValue.Fields[i].Value;
                        }
                    }

                    if (parameterNode == null)
                    {
                        throw new InvalidOperationException();
                    }

                    object? value =
                        operationType.Fields["parameter"].Type.ParseLiteral(parameterNode);

                    Expression nestedProperty;

                    if (field.Member is MethodInfo methodInfo)
                    {
                        nestedProperty = Expression.Call(
                            context.GetInstance(),
                            methodInfo,
                            new Expression[] { Expression.Constant(value) });
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    context.PushInstance(nestedProperty);
                    context.ClrTypes.Push(nestedProperty.Type);
                    context.TypeInfos.Push(field.TypeInfo);
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

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar).Ignore();
                descriptor.Operation(t => t.Complex(default)).Type<TestComplexInput>();
                descriptor.Operation(t => t.Simple());
            }
        }

        public class TestComplexInput : StringOperationInput
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