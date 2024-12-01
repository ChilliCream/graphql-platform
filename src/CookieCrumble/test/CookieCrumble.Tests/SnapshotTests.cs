using System.Buffers;
using System.Text;
using CookieCrumble.Formatters;
using CookieCrumble.Xunit;

namespace CookieCrumble;

public class SnapshotTests
{
    static SnapshotTests()
    {
        Snapshot.RegisterTestFramework(new XunitFramework());
    }

    [Fact]
    public void MatchSnapshot()
    {
        new MyClass().MatchSnapshot();
    }

    [Fact]
    public void OneSnapshot()
    {
        Snapshot.Match(new MyClass());
    }

    [Fact]
    public void OneSnapshot_Txt_Extension()
    {
        Snapshot.Match(new MyClass(), extension: ".txt");
    }

    [Fact]
    public void OneSnapshot_Post_Fix()
    {
        Snapshot.Match(new MyClass(), "ABC");
    }

    [Fact]
    public void TwoSnapshot()
    {
        Snapshot.Match(new MyClass(), new MyClass());
    }

    [Fact]
    public void ThreeSnapshot()
    {
        Snapshot.Match(new MyClass(), new MyClass(), new MyClass());
    }

    [Fact]
    public void SnapshotBuilder()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar", });
        snapshot.Add(new MyClass { Foo = "Baz", });
        snapshot.Match();
    }

    [Fact]
    public async Task SnapshotBuilderAsync()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar", });
        snapshot.Add(new MyClass { Foo = "Baz", });
        await snapshot.MatchAsync();
    }

    [Fact]
    public void SnapshotBuilder_Segment_Name()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar", }, "Bar:");
        snapshot.Add(new MyClass { Foo = "Baz", });
        snapshot.Match();
    }

    [Fact]
    public void SnapshotBuilder_Segment_Name_All()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass(), "Segment 1:");
        snapshot.Add(new MyClass { Foo = "Bar", }, "Segment 2:");
        snapshot.Add(new MyClass { Foo = "Baz", }, "Segment 3:");
        snapshot.Match();
    }

    [Fact]
    public void SnapshotBuilder_Segment_Custom_Serializer_For_Segment()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Baz", }, "Bar:", new CustomSerializer());
        snapshot.Add(new MyClass { Foo = "Baz", });
        snapshot.Add(new MyClass { Foo = "Baz", });
        snapshot.Match();
    }

    [Fact]
    public void SnapshotBuilder_Segment_Custom_Global_Serializer()
    {
        Snapshot.RegisterFormatter(new CustomSerializer());

        var snapshot = new Snapshot();
        snapshot.Add(new MyClass { Foo = "123", });
        snapshot.Match();
    }

    public class MyClass
    {
        public string Foo { get; set; } = "Bar";
    }

    public class CustomSerializer : ISnapshotValueFormatter
    {
        public bool CanHandle(object? value)
            => value is MyClass { Foo: "123", };

        public void Format(IBufferWriter<byte> snapshot, object? value)
        {
            var myClass = (MyClass)value!;
            Encoding.UTF8.GetBytes(myClass.Foo.AsSpan(), snapshot);
        }
    }
}
