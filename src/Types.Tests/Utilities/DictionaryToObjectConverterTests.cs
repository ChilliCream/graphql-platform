using System;
using System.Collections.Generic;
using ChilliCream.Testing;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Utilities
{
    public class DictionaryToObjectConverterTests
    {
        [Fact]
        public void Convert_Dictionary_FooObject()
        {
            // arrange
            var baz = new Dictionary<string, object>();
            baz["Number"] = "1.5";

            var bar = new Dictionary<string, object>();
            bar["State"] = "On";
            bar["Bazs"] = new List<object> { baz };

            var foo = new Dictionary<string, object>();
            foo["text"] = "abc";
            foo["BAR"] = bar;

            // assert
            var converter = new DictionaryToObjectConverter(
                TypeConversion.Default);
            object converted = converter.Convert(foo, typeof(Foo));

            // assert
            converted.Snapshot();
        }

        public class Foo
        {
            public string Text { get; set; }
            public Bar Bar { get; set; }
        }

        public class Bar
        {
            public State State { get; set; }
            public IReadOnlyCollection<Baz> Bazs { get; set; }
            public Baz[] BazArray { get; set; }
            public string[] StringArray { get; set; }
        }

        public class Baz
        {
            public decimal Number { get; set; }
        }

        public enum State
        {
            On,
            Off
        }
    }
}
