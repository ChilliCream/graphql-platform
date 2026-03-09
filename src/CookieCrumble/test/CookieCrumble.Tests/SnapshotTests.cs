using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using CookieCrumble.Formatters;
using CookieCrumble.Xunit;
using Xunit.Sdk;

namespace CookieCrumble;

public class SnapshotTests
{
    private const string StrictModeExceptionMessage =
        "Strict mode is enabled and no snapshot has been found "
        + "for the current test. Create a new snapshot locally and "
        + "rerun your tests.";

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
        snapshot.Add(new MyClass { Foo = "Bar" });
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Match();
    }

    [Fact]
    public async Task SnapshotBuilderAsync()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar" });
        snapshot.Add(new MyClass { Foo = "Baz" });
        await snapshot.MatchAsync();
    }

    [Fact]
    public void SnapshotBuilder_Segment_Name()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Bar" }, "Bar:");
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Match();
    }

    [Fact]
    public void SnapshotBuilder_Segment_Name_All()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass(), "Segment 1:");
        snapshot.Add(new MyClass { Foo = "Bar" }, "Segment 2:");
        snapshot.Add(new MyClass { Foo = "Baz" }, "Segment 3:");
        snapshot.Match();
    }

    [Fact]
    public void SnapshotBuilder_Segment_Custom_Serializer_For_Segment()
    {
        var snapshot = new Snapshot();
        snapshot.Add(new MyClass());
        snapshot.Add(new MyClass { Foo = "Baz" }, "Bar:", new CustomSerializer());
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Add(new MyClass { Foo = "Baz" });
        snapshot.Match();
    }

    [Fact]
    public void SnapshotBuilder_Segment_Custom_Global_Serializer()
    {
        Snapshot.RegisterFormatter(new CustomSerializer());

        var snapshot = new Snapshot();
        snapshot.Add(new MyClass { Foo = "123" });
        snapshot.Match();
    }

    [Theory]
    [InlineData("on")]
    [InlineData("true")]
    public async Task Match_StrictMode_On(string strictMode)
    {
        Environment.SetEnvironmentVariable("COOKIE_CRUMBLE_STRICT_MODE", strictMode);

        var snapshot = new Snapshot();
        snapshot.Add(new MyClass { Foo = "123" });

        async Task Act1() => await snapshot.MatchAsync();
        void Act2() => snapshot.Match();
        async Task Act3() => await snapshot.MatchMarkdownAsync();
        void Act4() => snapshot.MatchMarkdown();

        try
        {
            Assert.Equal(
                StrictModeExceptionMessage,
                (await Assert.ThrowsAsync<XunitException>(Act1)).Message);

            Assert.Equal(StrictModeExceptionMessage, Assert.Throws<XunitException>(Act2).Message);

            Assert.Equal(
                StrictModeExceptionMessage,
                (await Assert.ThrowsAsync<XunitException>(Act3)).Message);

            Assert.Equal(StrictModeExceptionMessage, Assert.Throws<XunitException>(Act4).Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("COOKIE_CRUMBLE_STRICT_MODE", null);
        }
    }

    [Theory]
    [InlineData(1, "off")]
    [InlineData(2, "false")]
    [InlineData(3, null)]
    public async Task Match_StrictMode_Off(int number, string? strictMode)
    {
        Environment.SetEnvironmentVariable("COOKIE_CRUMBLE_STRICT_MODE", strictMode);

        var snapshot = new Snapshot();
        snapshot.Add(new MyClass { Foo = "123" });

        async Task Act1() => await snapshot.SetPostFix($"MA_{number}").MatchAsync();
        void Act2() => snapshot.SetPostFix($"M_{number}").Match();
        async Task Act3() => await snapshot.SetPostFix($"MMA_{number}").MatchMarkdownAsync();
        void Act4() => snapshot.SetPostFix($"MM_{number}").MatchMarkdown();

        try
        {
            var result1 = await Record.ExceptionAsync(Act1);
            var result2 = Record.Exception(Act2);
            var result3 = await Record.ExceptionAsync(Act3);
            var result4 = Record.Exception(Act4);

            static string GetCallerFilePath([CallerFilePath] string filePath = "") => filePath;
            var directory = Path.GetDirectoryName(GetCallerFilePath()) + "/__snapshots__";

            File.Delete($"{directory}/SnapshotTests.Match_StrictMode_Off_MA_{number}.snap");
            File.Delete($"{directory}/SnapshotTests.Match_StrictMode_Off_M_{number}.snap");
            File.Delete($"{directory}/SnapshotTests.Match_StrictMode_Off_MMA_{number}.md");
            File.Delete($"{directory}/SnapshotTests.Match_StrictMode_Off_MM_{number}.md");

            Assert.Null(result1);
            Assert.Null(result2);
            Assert.Null(result3);
            Assert.Null(result4);
        }
        finally
        {
            Environment.SetEnvironmentVariable("COOKIE_CRUMBLE_STRICT_MODE", null);
        }
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
