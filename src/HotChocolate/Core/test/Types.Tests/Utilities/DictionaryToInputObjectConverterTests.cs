﻿using System.Collections.Generic;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Utilities
{
    public class DictionaryToInputObjectConverterTests
    {
        [Fact]
        public void Convert_Dictionary_FooInputObject()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<DummyQuery>()
                .AddType<InputObjectType<Foo>>()
                .AddType<DecimalType>()
                .Create();

            INamedInputType type = schema.GetType<INamedInputType>("FooInput");

            var baz = new Dictionary<string, object> { ["number"] = "1.5" };

            var bar = new Dictionary<string, object>
            {
                ["state"] = "ON",
                ["bazs"] = new List<object> { baz },
                ["stringArray"] = new List<object> { "1", "2", "5" }
            };

            var foo = new Dictionary<string, object>
            {
                ["text"] = "abc",
                ["bar"] = bar
            };

            // assert
            var converter = new DictionaryToInputObjectConverter(DefaultTypeConverter.Default);
            var converted = converter.Convert(foo, type);

            // assert
            converted.MatchSnapshot();
        }

        [Fact]
        public void Convert_Dictionary_InputObjectWithoutClrType()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<DummyQuery>()
                .AddInputObjectType(t =>
                    t.Name("FooInput")
                        .Field("a")
                        .Type<ListType<StringType>>())
                .Create();

            INamedInputType type = schema.GetType<INamedInputType>("FooInput");

            var foo = new Dictionary<string, object> { ["a"] = new List<object> { "abc", "def" } };

            // assert
            var converter = new DictionaryToInputObjectConverter(DefaultTypeConverter.Default);
            var converted = converter.Convert(foo, type);

            // assert
            converted.MatchSnapshot();
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

        public class DummyQuery
        {
            public string Foo { get; set; }
        }
    }
}
