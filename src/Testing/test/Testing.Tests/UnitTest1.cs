namespace Testing.Tests;

public class SnapshotTests
{
    [Fact]
    public void OneSnapshot()
    {
        Snapshot.Match(new MyClass());
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
        var snapshot = Snapshot.Create();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar" });
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Match();
    }

    [Fact]
    public async Task SnapshotBuilderAsync()
    {
        var snapshot = Snapshot.Create();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar" });
        snapshot.Add(new MyClass { Foo = "Baz" });
        await snapshot.MatchAsync();
    }

     [Fact]
    public void SnapshotBuilder_Segment_Name()
    {
        var snapshot = Snapshot.Create();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar" }, "Bar");
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Match();
    }

    public class MyClass
    {
        public string Foo { get; set; } = "Bar";
    }
}
