using System;
using System.Collections.Generic;
using ChilliCream.Testing;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Utilities
{
    public class DictionaryToInputObjectConverterTests
    {
        [Fact]
        public void Convert_Dictionary_FooInputObject()
        {
            // arrange
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType<InputObjectType<Foo>>();
            });

            var type = schema.GetType<INamedInputType>("FooInput");

            var baz = new Dictionary<string, object>();
            baz["number"] = "1.5";

            var bar = new Dictionary<string, object>();
            bar["state"] = "On";
            bar["bazs"] = new List<object> { baz };

            var foo = new Dictionary<string, object>();
            foo["text"] = "abc";
            foo["bar"] = bar;

            // assert
            var converter = new DictionaryToInputObjectConverter(
                TypeConversion.Default);
            object converted = converter.Convert(foo, type);

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
