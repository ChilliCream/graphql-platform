
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types
{
    public class InputObjectDeserializerTests
    {
        [Fact]
        public void ParseScalar()
        {
            // arrange
            IntType sourceType = new IntType();
            Type targetType = typeof(int);
            IntValueNode literal = new IntValueNode("1");

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<int>(result);
            Assert.Equal(1, result);
        }

        [Fact]
        public void ParseScalarAndConvert()
        {
            // arrange
            var sourceType = new IntType();
            var targetType = typeof(string);
            var literal = new IntValueNode("1");

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<string>(result);
            Assert.Equal("1", result);
        }

        [Fact]
        public void ParseNonNullScalar()
        {
            // arrange
            var sourceType = new NonNullType(new IntType());
            var targetType = typeof(int);
            IntValueNode literal = new IntValueNode("1");

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<int>(result);
            Assert.Equal(1, result);
        }

        [Fact]
        public void ParseScalarNullValue()
        {
            // arrange
            var sourceType = new IntType();
            var targetType = typeof(int);
            NullValueNode literal = NullValueNode.Default;

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseScalarListToArray()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(int[]);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<int[]>(result);
            Assert.Collection((int[])result, t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseScalarListToIEnumerable()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(IEnumerable<int>);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<List<int>>(result);
            Assert.Collection((IEnumerable<int>)result,
                t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseScalarListToICollection()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(ICollection<int>);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<List<int>>(result);
            Assert.Collection((ICollection<int>)result,
                t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseScalarListToIList()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(IList<int>);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<List<int>>(result);
            Assert.Collection((IList<int>)result,
                t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseScalarListToIReadOnlyCollection()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(IReadOnlyCollection<int>);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<List<int>>(result);
            Assert.Collection((IReadOnlyCollection<int>)result,
                t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseScalarListToIReadOnlyList()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(IReadOnlyList<int>);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<List<int>>(result);
            Assert.Collection((IReadOnlyList<int>)result,
                t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseScalarListToList()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(List<int>);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<List<int>>(result);
            Assert.Collection((List<int>)result,
                t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseScalarListToCollection()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(Collection<int>);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<Collection<int>>(result);
            Assert.Collection((Collection<int>)result,
                t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseScalarListToHashSet()
        {
            // arrange
            var sourceType = new ListType(new IntType());
            var targetType = typeof(HashSet<int>);
            var literal = new ListValueNode(new IntValueNode("1"));

            // act
            object result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal);

            // assert
            Assert.IsType<HashSet<int>>(result);
            Assert.Collection((HashSet<int>)result,
                t => Assert.Equal(1, t));
        }

        [Fact]
        public void ParseObject()
        {
            // arrange
            ISchema schema = CreateSchema();
            var sourceType = schema.GetType<INamedInputType>("FooInput");
            var targetType = typeof(Foo);
            var literal = CreateFoo();

            // act
            Foo result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal) as Foo;

            // assert
            Assert.NotNull(result);
            Assert.Equal("123", result.Bar1);
            Assert.Equal("456", result.Bar2);
        }

        [Fact]
        public void ParseAndMapObject()
        {
            // arrange
            ISchema schema = CreateSchema();
            var sourceType = schema.GetType<INamedInputType>("FooInput");
            var targetType = typeof(FooOnlyBar1);
            var literal = CreateFoo();

            // act
            FooOnlyBar1 result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal) as FooOnlyBar1;

            // assert
            Assert.NotNull(result);
            Assert.Equal("123", result.Bar1);
        }

        [Fact]
        public void ParseAndMapObjectAndConvert()
        {
            // arrange
            ISchema schema = CreateSchema();
            var sourceType = schema.GetType<INamedInputType>("FooInput");
            var targetType = typeof(FooOnlyBar1AsInt);
            var literal = CreateFoo();

            // act
            FooOnlyBar1AsInt result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal)
                    as FooOnlyBar1AsInt;

            // assert
            Assert.NotNull(result);
            Assert.Equal(123, result.Bar1);
        }

        [Fact]
        public void ParseObjectGraph()
        {
            // arrange
            ISchema schema = CreateSchema();
            var sourceType = schema.GetType<INamedInputType>("BarInput");
            var targetType = typeof(Bar);
            var literal = CreateBar();

            // act
            Bar result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal)
                    as Bar;

            // assert
            Assert.NotNull(result);
            Assert.NotNull(result.Foo);
            Assert.Equal("123", result.Foo.Bar1);
            Assert.Equal("456", result.Foo.Bar2);
        }

        [Fact]
        public void ParseObjectGraphWithList()
        {
            // arrange
            ISchema schema = CreateSchema();
            var sourceType = schema.GetType<INamedInputType>(
                "BarWithArrayInput");
            var targetType = typeof(BarWithArray);
            var literal = CreateBarWithArray();

            // act
            BarWithArray result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal)
                    as BarWithArray;

            // assert
            Assert.NotNull(result);
            Assert.NotNull(result.Foos);
            Assert.Collection(result.Foos,
                t =>
                {
                    Assert.Equal("123", t.Bar1);
                    Assert.Equal("456", t.Bar2);
                });
        }

        [Fact]
        public void ParseAndMapObjectGraphWithList()
        {
            // arrange
            ISchema schema = CreateSchema();
            var sourceType = schema.GetType<INamedInputType>(
                "BarWithArrayInput");
            var targetType = typeof(BarWithListOnlyGet);
            var literal = CreateBarWithArray();

            // act
            BarWithListOnlyGet result = ValueDeserializer
                .ParseLiteral(sourceType, targetType, literal)
                    as BarWithListOnlyGet;

            // assert
            Assert.NotNull(result);
            Assert.NotNull(result.Foos);
            Assert.Collection(result.Foos,
                t =>
                {
                    Assert.Equal("123", t.Bar1);
                    Assert.Equal("456", t.Bar2);
                });
        }

        private static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterType<InputObjectType<Foo>>();
                c.RegisterType<InputObjectType<Bar>>();
                c.RegisterType<InputObjectType<BarWithArray>>();
            });
        }

        private static IValueNode CreateFoo()
        {
            return new ObjectValueNode(
                new ObjectFieldNode("bar1", new StringValueNode("123")),
                new ObjectFieldNode("bar2", new StringValueNode("456")));
        }

        private static IValueNode CreateBar()
        {
            return new ObjectValueNode(
                new ObjectFieldNode("foo", CreateFoo()));
        }

        private static IValueNode CreateBarWithArray()
        {
            return new ObjectValueNode(
                new ObjectFieldNode("foos",
                    new ListValueNode(CreateFoo())));
        }

        public class Foo
        {
            public string Bar1 { get; set; }
            public string Bar2 { get; set; }
        }

        public class Bar
        {
            public Foo Foo { get; set; }
        }

        public class BarWithArray
        {
            public Foo[] Foos { get; set; }
        }

        public class BarWithListOnlyGet
        {
            public List<Foo> Foos { get; } = new List<Foo>();
        }

        public class FooOnlyBar1
        {
            public string Bar1 { get; set; }
        }

        public class FooOnlyBar1AsInt
        {
            public int Bar1 { get; set; }
        }


    }
}
