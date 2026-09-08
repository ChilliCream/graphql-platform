namespace HotChocolate.Types;

public class InterfaceObjectTests
{
    [Fact]
    public async Task Generic_Attribute_On_Static_Resolver_Class_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [EntityKey("id")]
            public sealed class Programme
            {
                public string Id { get; set; }
            }

            [InterfaceObject<Programme>]
            internal static partial class ProgrammeType
            {
                public static string[] GetAllowedUserActions([Parent] Programme programme)
                    => [];
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NonGeneric_Attribute_On_Runtime_Class_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [InterfaceObject]
            [EntityKey("id")]
            public sealed class Programme
            {
                public string Id { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NonGeneric_Attribute_Combined_With_ObjectType_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [EntityKey("id")]
            public sealed class Programme
            {
                public string Id { get; set; }
            }

            [ObjectType<Programme>]
            [InterfaceObject]
            internal static partial class ProgrammeType
            {
                public static string[] GetAllowedUserActions([Parent] Programme programme)
                    => [];
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generic_Attribute_On_NonStatic_Class_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [EntityKey("id")]
            public sealed class Programme
            {
                public string Id { get; set; }
            }

            [InterfaceObject<Programme>]
            internal partial class ProgrammeType
            {
                public string[] GetAllowedUserActions([Parent] Programme programme)
                    => [];
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Generic_Attribute_On_NonPartial_Class_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [EntityKey("id")]
            public sealed class Programme
            {
                public string Id { get; set; }
            }

            [InterfaceObject<Programme>]
            internal static class ProgrammeType
            {
                public static string[] GetAllowedUserActions([Parent] Programme programme)
                    => [];
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
