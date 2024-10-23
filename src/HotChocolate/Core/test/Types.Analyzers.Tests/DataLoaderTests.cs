namespace HotChocolate.Types;

public class DataLoaderTests
{
    [Fact]
    public async Task GenerateSource_BatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Group_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            [DataLoaderGroup("Group1")]
            internal static class TestClass
            {
                [DataLoader]
                [DataLoaderGroup("Group2")]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Group_Only_On_Class_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            [DataLoaderGroup("Group1")]
            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Group_Only_On_Method_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoaderGroup("Group1")]
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_GroupedDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<ILookup<int, Entity>> GetEntitiesByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_CacheDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Entity> GetEntityByIdAsync(
                    int entityId,
                    CancellationToken cancellationToken)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_GenericBatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IReadOnlyDictionary<int, T>> GetEntityByIdAsync<T>(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_IDictionary_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Lookup_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(Lookups = new string[] { nameof(CreateLookupKey) })]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;

                public static int CreateLookupKey(string key)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Lookup_From_OtherType_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(Lookups = new string[] { nameof(CreateLookupKey) })]
                public static Task<IDictionary<int, Entity2>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;

                public static KeyValuePair<int, Entity2> CreateLookupKey(Entity1 key)
                    => default!;
            }

            public class Entity1
            {
                public int Id { get; set; }

                public Entity2? Entity2 { get; set; }
            }

            public class Entity2
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_Nullable_Result_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Optional_State_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    [DataLoaderState("key")] string? state,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_Required_State_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    [DataLoaderState("key")] string state,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_State_With_Default_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<Dictionary<int, string?>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    [DataLoaderState("key")] string state = "default",
                    CancellationToken cancellationToken = default)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task DataLoader_With_Optional_Lookup()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader(Lookups = new string[] { nameof(CreateLookupKey) })]
                public static Task<IReadOnlyDictionary<int, Entity>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    CancellationToken cancellationToken)
                    => default!;

                public static int? CreateLookupKey(Entity entity)
                    => default!;
            }

            public class Entity
            {
                public int Id { get; set; }
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_PagingArguments_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    HotChocolate.Pagination.PagingArguments pagingArgs,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_SelectorBuilder_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Selectors.ISelectorBuilder selector,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BatchDataLoader_With_PredicateBuilder_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Predicates.IPredicateBuilder predicate,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_Without_Interface()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using GreenDonut;

            [assembly: GreenDonut.DataLoaderDefaults(GenerateInterfaces = false)]

            namespace TestNamespace;

            internal static class TestClass
            {
                [DataLoader]
                public static Task<IDictionary<int, string>> GetEntityByIdAsync(
                    IReadOnlyList<int> entityIds,
                    GreenDonut.Predicates.IPredicateBuilder predicate,
                    CancellationToken cancellationToken)
                    => default!;
            }
            """).MatchMarkdownAsync();
    }
}
