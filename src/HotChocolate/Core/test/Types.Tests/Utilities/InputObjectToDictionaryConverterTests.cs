using System.Collections.Generic;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities
{
    public class InputObjectToDictionaryConverterTests
    {
        [Fact]
        public void Convert_InputObject_Dictionary()
        {
            // arrange
            ISchema schema = Schema.Create(
                c =>
                {
                    c.RegisterQueryType<DummyQuery>();
                    c.RegisterType<InputObjectType<Foo>>();
                });

            InputObjectType type = schema.GetType<InputObjectType>("FooInput");

            var bar1 = new Bar { Number = 1, Baz = Baz.Bar };
            var bar2 = new Bar { Number = 2, Baz = Baz.Bar };
            var bar3 = new Bar { Number = 3, Baz = Baz.Foo };
            var foo = new Foo
            {
                Bar = bar1,
                Bars = new List<Bar> { bar2, bar3 }
            };

            // act
            var converter = new InputObjectToDictionaryConverter(
                DefaultTypeConverter.Default);
            Dictionary<string, object> dict = converter.Convert(type, foo);

            // assert
            dict.MatchSnapshot();
        }

        [Fact]
        public void Convert_InputObjectWithNullField_Dictionary()
        {
            // arrange
            ISchema schema = Schema.Create(
                c =>
                {
                    c.RegisterQueryType<DummyQuery>();
                    c.RegisterType<InputObjectType<Foo>>();
                });

            InputObjectType type = schema.GetType<InputObjectType>("FooInput");

            var bar2 = new Bar { Number = 2, Baz = Baz.Bar };
            var bar3 = new Bar { Number = 3, Baz = Baz.Foo };
            var foo = new Foo
            {
                Bar = null,
                Bars = new List<Bar> { bar2, bar3 }
            };

            // act
            var converter = new InputObjectToDictionaryConverter(
                DefaultTypeConverter.Default);
            Dictionary<string, object> dict = converter.Convert(type, foo);

            // assert
            dict.MatchSnapshot();
        }

        [Fact]
        public void Convert_InputObjectWithNullElement_Dictionary()
        {
            // arrange
            ISchema schema = Schema.Create(
                c =>
                {
                    c.RegisterQueryType<DummyQuery>();
                    c.RegisterType<InputObjectType<Foo>>();
                });

            InputObjectType type = schema.GetType<InputObjectType>("FooInput");

            var bar1 = new Bar { Number = 1, Baz = Baz.Bar };
            var bar2 = new Bar { Number = 2, Baz = Baz.Bar };
            var foo = new Foo
            {
                Bar = bar1,
                Bars = new List<Bar> { bar2, null }
            };

            // act
            var converter = new InputObjectToDictionaryConverter(
                DefaultTypeConverter.Default);
            Dictionary<string, object> dict = converter.Convert(type, foo);

            // assert
            dict.MatchSnapshot();
        }

        public class Foo
        {
            public List<Bar> Bars { get; set; }

            public Bar Bar { get; set; }
        }

        public class Bar
        {
            public int Number { get; set; }

            public Baz Baz { get; set; }
        }

        public enum Baz
        {
            Foo,
            Bar
        }

        public class DummyQuery
        {
            public string Foo { get; set; }
        }
    }
}
