using System;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterVisitorIdTests
        : FilterVisitorTestBase
    {
        [Fact]
        public void Create_ShortEqual_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ eq: \"{id}\" }}}}");
            ExecutorBuilder tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool> func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 12 };
            Assert.True(func(a));

            var b = new Foo { BarInt = 13 };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ShortNotEqual_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ neq: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);


            // assert
            var a = new Foo { BarInt = 13 };
            Assert.True(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.False(func(b));
        }


        [Fact]
        public void Create_ShortGreaterThan_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ gt: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 11 };
            Assert.False(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.False(func(b));

            var c = new Foo { BarInt = 13 };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_ShortNotGreaterThan_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ ngt: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 11 };
            Assert.True(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.True(func(b));

            var c = new Foo { BarInt = 13 };
            Assert.False(func(c));
        }


        [Fact]
        public void Create_ShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ gte: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 11 };
            Assert.False(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.True(func(b));

            var c = new Foo { BarInt = 13 };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_ShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ ngte: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 11 };
            Assert.True(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.False(func(b));

            var c = new Foo { BarInt = 13 };
            Assert.False(func(c));
        }



        [Fact]
        public void Create_ShortLowerThan_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ lt: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 11 };
            Assert.True(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.False(func(b));

            var c = new Foo { BarInt = 13 };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_ShortNotLowerThan_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ nlt: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 11 };
            Assert.False(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.True(func(b));

            var c = new Foo { BarInt = 13 };
            Assert.True(func(c));
        }


        [Fact]
        public void Create_ShortLowerThanOrEquals_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ lte: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 11 };
            Assert.True(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.True(func(b));

            var c = new Foo { BarInt = 13 };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_ShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ nlte: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 11 };
            Assert.False(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.False(func(b));

            var c = new Foo { BarInt = 13 };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_ShortIn_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var first = serializer.Serialize("Foo", 13);
            var second = serializer.Serialize("Foo", 14);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ in: [\"{first}\", \"{second}\"] }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 13 };
            Assert.True(func(a));

            var b = new Foo { BarInt = 12 };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ShortNotIn_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var first = serializer.Serialize("Foo", 13);
            var second = serializer.Serialize("Foo", 14);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ nin: [\"{first}\", \"{second}\"] }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarInt = 12 };
            Assert.True(func(a));

            var b = new Foo { BarInt = 13 };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_NullableShortEqual_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ eq: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 12 };
            Assert.True(func(a));

            var b = new FooNullable { BarInt = 13 };
            Assert.False(func(b));

            var c = new FooNullable { BarInt = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableShortNotEqual_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ neq: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 13 };
            Assert.True(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarInt = null };
            Assert.True(func(c));
        }


        [Fact]
        public void Create_NullableShortGreaterThan_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ gt: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 11 };
            Assert.False(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarInt = 13 };
            Assert.True(func(c));

            var d = new FooNullable { BarInt = null };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_NullableShortNotGreaterThan_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ ngt: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 11 };
            Assert.True(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.True(func(b));

            var c = new FooNullable { BarInt = 13 };
            Assert.False(func(c));

            var d = new FooNullable { BarInt = null };
            Assert.True(func(d));
        }

        [Fact]
        public void Create_NullableShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ gte: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 11 };
            Assert.False(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.True(func(b));

            var c = new FooNullable { BarInt = 13 };
            Assert.True(func(c));

            var d = new FooNullable { BarInt = null };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_NullableShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ ngte: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 11 };
            Assert.True(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarInt = 13 };
            Assert.False(func(c));

            var d = new FooNullable { BarInt = null };
            Assert.True(func(d));
        }



        [Fact]
        public void Create_NullableShortLowerThan_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ lt: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 11 };
            Assert.True(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarInt = 13 };
            Assert.False(func(c));

            var d = new FooNullable { BarInt = null };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_NullableShortNotLowerThan_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ nlt: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 11 };
            Assert.False(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.True(func(b));

            var c = new FooNullable { BarInt = 13 };
            Assert.True(func(c));

            var d = new FooNullable { BarInt = null };
            Assert.True(func(d));
        }


        [Fact]
        public void Create_NullableShortLowerThanOrEquals_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ lte: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 11 };
            Assert.True(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.True(func(b));

            var c = new FooNullable { BarInt = 13 };
            Assert.False(func(c));

            var d = new FooNullable { BarInt = null };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_NullableShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var id = serializer.Serialize("Foo", 12);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ nlte: \"{id}\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 11 };
            Assert.False(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarInt = 13 };
            Assert.True(func(c));

            var d = new FooNullable { BarInt = null };
            Assert.True(func(d));
        }

        [Fact]
        public void Create_NullableShortIn_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var first = serializer.Serialize("Foo", 13);
            var second = serializer.Serialize("Foo", 14);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ in: [\"{first}\", \"{second}\"] }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 13 };
            Assert.True(func(a));

            var b = new FooNullable { BarInt = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarInt = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableShortNotIn_Expression()
        {
            // arrange
            var serializer = new IdSerializer();
            var first = serializer.Serialize("Foo", 13);
            var second = serializer.Serialize("Foo", 14);
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                $"{{ barInt: {{ nin: [\"{first}\", \"{second}\"] }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterInput());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarInt = 12 };
            Assert.True(func(a));

            var b = new FooNullable { BarInt = 13 };
            Assert.False(func(b));

            var c = new FooNullable { BarInt = null };
            Assert.True(func(c));
        }

        [Fact]
        public void Overwrite_Comparable_Filter_Type_With_Attribute()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FilterInputType<EntityWithTypeAttribute>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Foo
        {
            public int BarInt { get; set; }
        }

        public class FooNullable
        {
            public short? BarInt { get; set; }
        }

        public class FooFilterInput
            : FilterInputType<Foo>
        {
            protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(x => x.BarInt).Type<IdOperationFilterInputType<Foo, int>>();
            }
        }

        public class FooNullableFilterInput
            : FilterInputType<FooNullable>
        {
            protected override void Configure(IFilterInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Field(x => x.BarInt).Type<IdOperationFilterInputType<Foo, int>>();
            }
        }

        public class EntityWithTypeAttribute
        {
            [GraphQLType(typeof(IntType))]
            public short? BarInt { get; set; }
        }

        public class Entity
        {
            public short? BarInt { get; set; }
        }

    }
}
