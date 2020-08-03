using System;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{

    public class QueryableFilterVisitorEnumTests
        : FilterVisitorTestBase
    {
        [Fact]
        public void Create_EnumEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ barEnum: { eq: BAR }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarEnum = FooEnum.BAR };
            Assert.True(func(a));

            var b = new Foo { BarEnum = FooEnum.BAZ };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_EnumNotEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ barEnum: { neq: BAR }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);


            // assert
            var a = new Foo { BarEnum = FooEnum.BAZ };
            Assert.True(func(a));

            var b = new Foo { BarEnum = FooEnum.BAR };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_EnumIn_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ barEnum: { in: [BAZ, QUX] }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarEnum = FooEnum.BAZ };
            Assert.True(func(a));

            var b = new Foo { BarEnum = FooEnum.BAR };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_EnumNotIn_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ barEnum: { nin: [BAZ, QUX] }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { BarEnum = FooEnum.BAR };
            Assert.True(func(a));

            var b = new Foo { BarEnum = FooEnum.BAZ };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_NullableEnumEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ barEnum: { eq: BAR }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterType());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarEnum = FooEnum.BAR };
            Assert.True(func(a));

            var b = new FooNullable { BarEnum = FooEnum.BAZ };
            Assert.False(func(b));

            var c = new FooNullable { BarEnum = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableEnumNotEqual_Expression()
        {
            // arrange
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ barEnum: { neq: BAR }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterType());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarEnum = FooEnum.BAZ };
            Assert.True(func(a));

            var b = new FooNullable { BarEnum = FooEnum.BAR };
            Assert.False(func(b));

            var c = new FooNullable { BarEnum = null };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_NullableEnumIn_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ barEnum: { in: [BAZ, QUX] }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterType());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarEnum = FooEnum.BAZ };
            Assert.True(func(a));

            var b = new FooNullable { BarEnum = FooEnum.BAR };
            Assert.False(func(b));

            var c = new FooNullable { BarEnum = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableEnumNotIn_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ barEnum: { nin: [BAZ, QUX] }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterType());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { BarEnum = FooEnum.BAR };
            Assert.True(func(a));

            var b = new FooNullable { BarEnum = FooEnum.BAZ };
            Assert.False(func(b));

            var c = new FooNullable { BarEnum = null };
            Assert.True(func(c));
        }

        [Fact]
        public void Overwrite_Enum_Filter_Type_With_Attribute()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FilterInputType<EntityWithTypeAttribute>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Foo
        {
            public FooEnum BarEnum { get; set; }
        }

        public class FooNullable
        {
            public FooEnum? BarEnum { get; set; }
        }

        public enum FooEnum
        {
            FOO,
            BAR,
            BAZ,
            QUX
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }

        public class EntityWithTypeAttribute
        {
            [GraphQLType(typeof(IntType))]
            public short? BarEnum { get; set; }
        }

        public class Entity
        {
            public short? BarEnum { get; set; }
        }

    }
}