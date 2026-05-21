namespace HotChocolate.Types;

public class CustomDescriptorAttributeTests
{
    private const string PrefixAttributeSource =
        """
        using System;
        using HotChocolate.Types;
        using HotChocolate.Types.Descriptors;

        namespace TestNamespace;

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public sealed class PrefixAttribute(string prefix) : ObjectTypeDescriptorAttribute
        {
            public string Prefix { get; } = prefix;

            protected override void OnConfigure(
                IDescriptorContext context,
                IObjectTypeDescriptor descriptor,
                Type? type)
            {
                if (type is null) return;
                var captured = Prefix;
                descriptor.Extend().OnBeforeNaming((_, cfg) => cfg.Name = captured + "_" + cfg.Name);
            }
        }
        """;

    [Fact]
    public async Task CustomDescriptorAttribute_On_QueryType_Partial()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                PrefixAttributeSource,
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                [QueryType]
                [Prefix("scoped")]
                public static partial class Query
                {
                    public static string Foo() => "bar";
                }
                """
            ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomDescriptorAttribute_SameValue_On_Two_QueryType_Partials_DeduplicatedKey()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                PrefixAttributeSource,
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                [QueryType]
                [Prefix("scoped")]
                public static partial class QueryPartA
                {
                    public static string Foo() => "bar";
                }

                [QueryType]
                [Prefix("scoped")]
                public static partial class QueryPartB
                {
                    public static string Baz() => "qux";
                }
                """
            ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomDescriptorAttribute_DifferentValues_On_Two_QueryType_Partials_DistinctKeys()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                PrefixAttributeSource,
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                [QueryType]
                [Prefix("first")]
                public static partial class QueryPartA
                {
                    public static string Foo() => "bar";
                }

                [QueryType]
                [Prefix("second")]
                public static partial class QueryPartB
                {
                    public static string Baz() => "qux";
                }
                """
            ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomDescriptorAttribute_On_ObjectType_Partial()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                PrefixAttributeSource,
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                public sealed class Book
                {
                    public required string Id { get; set; }
                }

                [ObjectType<Book>]
                [Prefix("scoped")]
                public static partial class BookNode
                {
                    public static string Subtitle() => "demo";
                }
                """
            ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomDescriptorAttribute_SameValue_On_Two_ObjectType_Partials_DeduplicatedKey()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                PrefixAttributeSource,
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                public sealed class Book
                {
                    public required string Id { get; set; }
                }

                [ObjectType<Book>]
                [Prefix("scoped")]
                public static partial class BookNodeA
                {
                    public static string Subtitle() => "demo";
                }

                [ObjectType<Book>]
                [Prefix("scoped")]
                public static partial class BookNodeB
                {
                    public static string Tagline() => "demo";
                }
                """
            ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomDescriptorAttribute_DifferentValues_On_Two_ObjectType_Partials_DistinctKeys()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                PrefixAttributeSource,
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                public sealed class Book
                {
                    public required string Id { get; set; }
                }

                [ObjectType<Book>]
                [Prefix("first")]
                public static partial class BookNodeA
                {
                    public static string Subtitle() => "demo";
                }

                [ObjectType<Book>]
                [Prefix("second")]
                public static partial class BookNodeB
                {
                    public static string Tagline() => "demo";
                }
                """
            ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomDescriptorAttribute_MultipleDistinctAttributes_OnOnePartial()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                PrefixAttributeSource,
                """
                using System;
                using HotChocolate;
                using HotChocolate.Types;
                using HotChocolate.Types.Descriptors;

                namespace TestNamespace;

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
                public sealed class SuffixAttribute(string suffix) : ObjectTypeDescriptorAttribute
                {
                    public string Suffix { get; } = suffix;

                    protected override void OnConfigure(
                        IDescriptorContext context,
                        IObjectTypeDescriptor descriptor,
                        Type? type)
                    {
                        if (type is null) return;
                        var captured = Suffix;
                        descriptor.Extend().OnBeforeNaming((_, cfg) => cfg.Name = cfg.Name + "_" + captured);
                    }
                }

                [QueryType]
                [Prefix("p")]
                [Suffix("s")]
                public static partial class Query
                {
                    public static string Foo() => "bar";
                }
                """
            ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomDescriptorAttribute_With_NamedArguments_OnPartial()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using System;
                using HotChocolate.Types;
                using HotChocolate.Types.Descriptors;

                namespace TestNamespace;

                [AttributeUsage(AttributeTargets.Class)]
                public sealed class TagAttribute : ObjectTypeDescriptorAttribute
                {
                    public string? Label { get; set; }
                    public int Weight { get; set; }

                    protected override void OnConfigure(
                        IDescriptorContext context,
                        IObjectTypeDescriptor descriptor,
                        Type? type) { }
                }
                """,
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                [QueryType]
                [Tag(Label = "primary", Weight = 10)]
                public static partial class Query
                {
                    public static string Foo() => "bar";
                }
                """
            ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CustomDescriptorAttribute_On_InterfaceType_Partial()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            [
                """
                using System;
                using HotChocolate.Types;
                using HotChocolate.Types.Descriptors;

                namespace TestNamespace;

                [AttributeUsage(AttributeTargets.Class)]
                public sealed class IfacePrefixAttribute(string prefix) : InterfaceTypeDescriptorAttribute
                {
                    public string Prefix { get; } = prefix;

                    protected override void OnConfigure(
                        IDescriptorContext context,
                        IInterfaceTypeDescriptor descriptor,
                        Type? type)
                    {
                        if (type is null) return;
                        var captured = Prefix;
                        descriptor.Extend().OnBeforeNaming((_, cfg) => cfg.Name = captured + "_" + cfg.Name);
                    }
                }
                """,
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                public interface IEntity
                {
                    string Id { get; }
                }

                [InterfaceType<IEntity>]
                [IfacePrefix("scoped")]
                public static partial class EntityInterface
                {
                    public static string Kind() => "entity";
                }
                """
            ]).MatchMarkdownAsync();
    }
}
