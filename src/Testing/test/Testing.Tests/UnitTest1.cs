namespace Testing.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var snapshot = Snapshot.Create();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass());
        await snapshot.MatchAsync();
    }

    public class MyClass
    {
        public string Foo => "Bar";
    }
}
