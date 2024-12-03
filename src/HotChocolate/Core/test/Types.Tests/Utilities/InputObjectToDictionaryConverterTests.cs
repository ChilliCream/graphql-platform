using HotChocolate.Types;

namespace HotChocolate.Utilities;

public class InputObjectToDictionaryConverterTests
{
    [Fact]
    public void Convert_InputObject_Dictionary()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<InputObjectType<Foo>>()
            .Create();

        var type = schema.GetType<InputObjectType>("FooInput");

        var bar1 = new Bar { Number = 1, Baz = Baz.Bar, };
        var bar2 = new Bar { Number = 2, Baz = Baz.Bar, };
        var bar3 = new Bar { Number = 3, Baz = Baz.Foo, };
        var foo = new Foo
        {
            Bar = bar1,
            Bars = [bar2, bar3,],
        };

        // act
        var converter = new InputObjectToDictionaryConverter(
            DefaultTypeConverter.Default);
        var dict = converter.Convert(type, foo);

        // assert
        dict.MatchSnapshot();
    }

    [Fact]
    public void Convert_InputObjectWithNullField_Dictionary()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<InputObjectType<Foo>>()
            .Create();

        var type = schema.GetType<InputObjectType>("FooInput");

        var bar2 = new Bar { Number = 2, Baz = Baz.Bar, };
        var bar3 = new Bar { Number = 3, Baz = Baz.Foo, };
        var foo = new Foo
        {
            Bar = null,
            Bars = [bar2, bar3,],
        };

        // act
        var converter = new InputObjectToDictionaryConverter(
            DefaultTypeConverter.Default);
        var dict = converter.Convert(type, foo);

        // assert
        dict.MatchSnapshot();
    }

    [Fact]
    public void Convert_InputObjectWithNullElement_Dictionary()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<InputObjectType<Foo>>()
            .Create();

        var type = schema.GetType<InputObjectType>("FooInput");

        var bar1 = new Bar { Number = 1, Baz = Baz.Bar, };
        var bar2 = new Bar { Number = 2, Baz = Baz.Bar, };
        var foo = new Foo
        {
            Bar = bar1,
            Bars = [bar2, null,],
        };

        // act
        var converter = new InputObjectToDictionaryConverter(
            DefaultTypeConverter.Default);
        var dict = converter.Convert(type, foo);

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
        Bar,
    }

    public class DummyQuery
    {
        public string Foo { get; set; }
    }
}
