using System.Buffers;
using System.Text;
using CookieCrumble.Formatters;
using HotChocolate.Language;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CookieCrumble.MSTests;

[TestClass]
public class SnapshotMSTests
{
    [TestInitialize()]
    public void Initialize()
    {
        CookieCrumble.Snapshot.RegisterTestFramework(new CookieCrumble.MSTest.MSTestFramework());
    }

    [TestMethod]
    public void MatchSnapshot()
    {
        new MyClass().MatchSnapshot();
    }

    [TestMethod]
    public void OneSnapshot()
    {
        Snapshot.Match(new MyClass());
    }

    [TestMethod]
    public void OneSnapshot_Txt_Extension()
    {
        Snapshot.Match(new MyClass(), extension: ".txt");
    }

    [TestMethod]
    public void OneSnapshot_Post_Fix()
    {
        Snapshot.Match(new MyClass(), "ABC");
    }

    [TestMethod]
    public void TwoSnapshot()
    {
        Snapshot.Match(new MyClass(), new MyClass());
    }

    [TestMethod]
    public void ThreeSnapshot()
    {
        Snapshot.Match(new MyClass(), new MyClass(), new MyClass());
    }

    [TestMethod]
    public void SnapshotBuilder()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar" });
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Match();
    }

    [TestMethod]
    public async Task SnapshotBuilderAsync()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar" });
        snapshot.Add(new MyClass { Foo = "Baz" });
        await snapshot.MatchAsync();
    }

    [TestMethod]
    public void SnapshotBuilder_Segment_Name()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar" }, "Bar:");
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Match();
    }

    [TestMethod]
    public void SnapshotBuilder_Segment_Name_All()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass(), "Segment 1:");
        snapshot.Add(new MyClass { Foo = "Bar" }, "Segment 2:");
        snapshot.Add(new MyClass { Foo = "Baz" }, "Segment 3:");
        snapshot.Match();
    }

    [TestMethod]
    public void SnapshotBuilder_Segment_Custom_Serializer_For_Segment()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Baz" }, "Bar:", new CustomSerializer());
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Match();
    }

    [TestMethod]
    public void SnapshotBuilder_Segment_Custom_Global_Serializer()
    {
        Snapshot.RegisterFormatter(new CustomSerializer());

        var snapshot = new Snapshot();
        snapshot.Add(new MyClass { Foo = "123" });
        snapshot.Match();
    }

    [TestMethod]
    public void SnapshotBuilder_GraphQL_Segment()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass { Foo = "def" });
        snapshot.Add(Utf8GraphQLParser.Parse("{ abc }"));
        snapshot.Match();
    }

    public class MyClass
    {
        public string Foo { get; set; } = "Bar";
    }

    public class CustomSerializer : ISnapshotValueFormatter
    {
        public bool CanHandle(object? value)
            => value is MyClass { Foo: "123" };

        public void Format(IBufferWriter<byte> snapshot, object? value)
        {
            var myClass = (MyClass)value!;
            Encoding.UTF8.GetBytes(myClass.Foo.AsSpan(), snapshot);
        }
    }
}
