namespace HotChocolate.Types;

public class ConflictingIdAttributeAnalyzerTests
{
    [Fact]
    public async Task Analyze_Should_RaiseErrorOnMismatchedMember_When_IdTypesDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int Id { get; set; }

                [ID]
                public Guid AnotherTypeId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_ExplicitTypeNameIsSpecified()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int Id { get; set; }

                [ID("Foo")]
                public Guid AnotherTypeId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_GenericIdAttributeIsUsed()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int Id { get; set; }

                [ID<Thing>]
                public Guid AnotherTypeId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_IdTypesMatch()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int Id { get; set; }

                [ID]
                public int AnotherTypeId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_NullableMatchesNonNullable()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int Id { get; set; }

                [ID]
                public int? AnotherTypeId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_EnumerableElementMatchesScalar()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System.Collections.Generic;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int Id { get; set; }

                [ID]
                public IEnumerable<int> AnotherTypeId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_InterfaceHasMismatchedIds()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [InterfaceType<IThing>]
            public static partial class ThingType
            {
            }

            public interface IThing
            {
                [ID]
                int Id { get; }

                [ID]
                Guid AnotherTypeId { get; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseErrorOnMismatchedMethod_When_IdMethodTypesDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int GetId() => 1;

                [ID]
                public Guid GetOther() => Guid.NewGuid();
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_FlagSecondMember_When_NoMemberNamedId()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int FirstId { get; set; }

                [ID]
                public Guid SecondId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_FlagAllExceptIdMember_When_IdMemberIsNotFirst()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public Guid FooId { get; set; }

                [ID]
                public int Id { get; set; }

                [ID]
                public string BarId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_EnumerableInnerTypesDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using System.Collections.Generic;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public IEnumerable<int> Id { get; set; }

                [ID]
                public IEnumerable<Guid> OtherIds { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_EnumerableInnerTypesMatch()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System.Collections.Generic;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public IEnumerable<int> Id { get; set; }

                [ID]
                public IEnumerable<int> OtherIds { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_SameGenericTypeNameHasMismatchedIdTypes()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Foo;

            public class Thing
            {
                [ID<Foo>]
                public int Id { get; set; }

                [ID<Foo>]
                public Guid Other { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_SameTypeNameStringHasMismatchedIdTypes()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID("Shared")]
                public int Id { get; set; }

                [ID("Shared")]
                public Guid Other { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_TypeNameStringsDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID("A")]
                public int Id { get; set; }

                [ID("B")]
                public Guid Other { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_GenericTypeArgumentsDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Foo;

            public class Bar;

            public class Thing
            {
                [ID<Foo>]
                public int Id { get; set; }

                [ID<Bar>]
                public Guid Other { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_ArrayElementTypesDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int[] Id { get; set; }

                [ID]
                public Guid[] OtherIds { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_ListElementTypesDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using System.Collections.Generic;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public List<int> Id { get; set; }

                [ID]
                public List<Guid> OtherIds { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_CollectionShapesShareElementType()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System.Collections.Generic;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int[] Id { get; set; }

                [ID]
                public IEnumerable<int> Two { get; set; }

                [ID]
                public List<int> Three { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_TaskResultTypesDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using System.Threading.Tasks;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public Task<int> GetId() => Task.FromResult(1);

                [ID]
                public Task<Guid> GetOther() => Task.FromResult(Guid.NewGuid());
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_NotRaiseError_When_ValueTaskResultMatchesScalar()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System.Threading.Tasks;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public ValueTask<int> GetId() => new ValueTask<int>(1);

                [ID]
                public int Other { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_NullableValueTypesDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public int? Id { get; set; }

                [ID]
                public Guid? OtherId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Analyze_Should_RaiseError_When_NestedTaskListElementTypesDiffer()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Thing>]
            public static partial class ThingType
            {
            }

            public class Thing
            {
                [ID]
                public Task<List<int>> GetId() => Task.FromResult(new List<int>());

                [ID]
                public Task<List<Guid>> GetOther() => Task.FromResult(new List<Guid>());
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
