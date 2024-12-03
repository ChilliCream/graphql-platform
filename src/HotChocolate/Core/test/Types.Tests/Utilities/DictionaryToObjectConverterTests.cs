namespace HotChocolate.Utilities;

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
        bar["Bazs"] = new List<object> { baz, };

        var foo = new Dictionary<string, object>();
        foo["text"] = "abc";
        foo["BAR"] = bar;

        // assert
        var converter = new DictionaryToObjectConverter(
            DefaultTypeConverter.Default);
        var converted = converter.Convert(foo, typeof(Foo));

        // assert
        converted.MatchSnapshot();
    }

    [Fact]
    public void Convert_Dictionary_BarObjectWithArray()
    {
        // arrange
        var baz = new Dictionary<string, object>();
        baz["Number"] = "1.5";

        var bar = new Dictionary<string, object>();
        bar["State"] = "On";
        bar["Bazs"] = new List<object> { baz, };
        bar["BazArray"] = new List<object> { baz, };
        bar["StringArray"] = new List<object> { "a", 1, true, };

        // assert
        var converter = new DictionaryToObjectConverter(
            DefaultTypeConverter.Default);
        var converted = converter.Convert(bar, typeof(Bar));

        // assert
        converted.MatchSnapshot();
    }

    [Fact]
    public void Convert_List_ListOfBar()
    {
        // arrange
        var baz = new Dictionary<string, object>();
        baz["Number"] = "1.5";

        var bar = new Dictionary<string, object>();
        bar["State"] = "On";
        bar["Bazs"] = new List<object> { baz, };
        bar["BazArray"] = new List<object> { baz, };
        bar["StringArray"] = new List<object> { "a", 1, true, };

        var list = new List<object> { bar, };

        // assert
        var converter = new DictionaryToObjectConverter(
            DefaultTypeConverter.Default);
        var converted = converter.Convert(
            list, typeof(ICollection<Bar>));

        // assert
        converted.MatchSnapshot();
    }

    [Fact]
    public void Convert_String_Int()
    {
        // arrange
        var input = "1";

        // assert
        var converter = new DictionaryToObjectConverter(
            DefaultTypeConverter.Default);
        var converted = converter.Convert(
            input, typeof(int));

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
        Off,
    }
}
