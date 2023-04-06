using System.Collections.Generic;
using System.Dynamic;

using Snapshooter.Xunit;

using Xunit;

namespace HotChocolate.Utilities
{
    public class ObjectToDictionaryConverterTests
    {
        [Fact]

        public void Convert_Object_Dictionary()
        {
            // arrange
            var bar1 = new Bar { Number = 1, Baz = Baz.Bar };
            var bar2 = new Bar { Number = 2, Baz = Baz.Bar };
            var bar3 = new Bar { Number = 3, Baz = Baz.Foo };
            var foo = new Foo
            {
                Bar = bar1,
                Bars = new List<Bar> { bar2, bar3 }
            };

            // act
            var converter = new ObjectToDictionaryConverter(
                DefaultTypeConverter.Default);
            var dict = converter.Convert(foo);

            // assert
            dict.MatchSnapshot();
        }

        [Fact]
        public void Convert_ObjectWithNullField_Dictionary()
        {
            // arrange
            var bar2 = new Bar { Number = 2, Baz = Baz.Bar };
            var bar3 = new Bar { Number = 3, Baz = Baz.Foo };
            var foo = new Foo
            {
                Bar = null,
                Bars = new List<Bar> { bar2, bar3 }
            };

            // act
            var converter = new ObjectToDictionaryConverter(
                DefaultTypeConverter.Default);
            var dict = converter.Convert(foo);

            // assert
            dict.MatchSnapshot();
        }

        [Fact]
        public void Convert_ObjectWithNullElement_Dictionary()
        {
            // arrange
            var bar1 = new Bar { Number = 1, Baz = Baz.Bar };
            var bar2 = new Bar { Number = 2, Baz = Baz.Bar };
            var foo = new Foo
            {
                Bar = bar1,
                Bars = new List<Bar> { bar2, null }
            };

            // act
            var converter = new ObjectToDictionaryConverter(
                DefaultTypeConverter.Default);
            var dict = converter.Convert(foo);

            // assert
            dict.MatchSnapshot();
        }

        [Fact]
        public void Convert_IReadOnlyDictionary_Dictionary()
        {
            // arrange
            var bar1 = new Bar { Number = 1, Baz = Baz.Bar };
            var bar2 = new Bar { Number = 2, Baz = Baz.Bar };

            // it implements both IReadOnlyDictionary and IDictionary
            var foo = new Dictionary<string, object>
            {
                ["Bar"] = bar1,
                ["Bars"] = new List<Bar> { bar2, null }
            };

            // act
            var converter = new ObjectToDictionaryConverter(
                DefaultTypeConverter.Default);
            var dict = converter.Convert(foo);

            // assert
            dict.MatchSnapshot();
        }

        [Fact]
        public void Convert_IDictionary_Dictionary()
        {
            // arrange
            var bar1 = new Bar { Number = 1, Baz = Baz.Bar };
            var bar2 = new Bar { Number = 2, Baz = Baz.Bar };
            IDictionary<string, object?> foo = new ExpandoObject(); // it doesn't implement IReadOnlyDictionary
            foo["Bar"] = bar1;
            foo["Bars"] = new List<Bar> { bar2, null };

            // act
            var converter = new ObjectToDictionaryConverter(
                DefaultTypeConverter.Default);
            var dict = converter.Convert(foo);

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
