namespace HotChocolate.Types;

public class IdAttributeOnRecordParameterAnalyzerTests
{
    [Fact]
    public async Task RecordWithPropertyTargetedIdAttribute_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Author>]
            public static partial class AuthorType
            {
            }

            public record Author([property: ID] int Id, string Name);
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task RecordWithIdAttributeOnParameter_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Author>]
            public static partial class AuthorType
            {
            }

            public record Author([ID] int Id, string Name);
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task RecordWithMultipleAttributes_OnlyIdRaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.ComponentModel;

            namespace TestNamespace;

            [ObjectType<Author>]
            public static partial class AuthorType
            {
            }

            public record Author([ID][Description("The author ID")] int Id, string Name);
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task RegularClass_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Author>]
            public static partial class AuthorType
            {
            }

            public class Author
            {
                [ID]
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task RecordWithMultipleIdParameters_RaisesMultipleErrors()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<AuthorBook>]
            public static partial class AuthorBookType
            {
            }

            public record AuthorBook([ID] int AuthorId, [ID] int BookId);
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task RecordWithMixedAttributes_OnlyParameterTargetedRaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Author>]
            public static partial class AuthorType
            {
            }

            public record Author([ID] int Id, [property: ID] int SecondaryId);
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
