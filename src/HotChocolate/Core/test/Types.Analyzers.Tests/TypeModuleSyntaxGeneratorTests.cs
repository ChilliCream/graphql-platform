namespace HotChocolate.Types;

public class TypeModuleSyntaxGeneratorTests
{
    [Fact]
    public async Task GenerateSource_TypeModuleOrdering_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            internal class ATestBType: ObjectType<ATestB>;
            internal record ATestB(int Id);
            """,
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            internal class ATestAType : ObjectType<ATestA>;
            internal record ATestA(int Id);
            """,
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<ATestBAttr>]
            internal static partial class ATestBAttrType;
            internal record ATestBAttr(int Id);
            """,
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<ATestAAttr>]
            internal static partial class ATestAAttrType;
            internal record ATestAAttr(int Id);
            """,
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            internal class ATestBExtType : ObjectTypeExtension<ATestBExt>;
            internal record ATestBExt(int Id);
            """,
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            internal class ATestAExtType : ObjectTypeExtension<ATestAExt>;
            internal record ATestAExt(int Id);
            """,
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            [ExtendObjectType<ATestBExtAttr>]
            internal class ATestBExtAttrType;
            internal record ATestBExtAttr(int Id);
            """,
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            [ExtendObjectType<ATestAExtAttr>]
            internal class ATestAExtAttrType;
            internal record ATestAExtAttr(int Id);
            """,
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal class TestBDataLoader(
                IBatchScheduler batchScheduler,
                DataLoaderOptions options)
                : BatchDataLoader<int, object>(batchScheduler, options)
            {
                protected override async Task<IReadOnlyDictionary<int, object>> LoadBatchAsync(
                    IReadOnlyList<int> ids,
                    CancellationToken cancellationToken)
                {
                    return await Task.FromResult(new Dictionary<int, object>());
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal class TestADataLoader(
                IBatchScheduler batchScheduler,
                DataLoaderOptions options)
                : BatchDataLoader<int, object>(batchScheduler, options)
            {
                protected override async Task<IReadOnlyDictionary<int, object>> LoadBatchAsync(
                    IReadOnlyList<int> ids,
                    CancellationToken cancellationToken)
                {
                    return await Task.FromResult(new Dictionary<int, object>());
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestBDataLoaderAttr
            {
                [DataLoader]
                public static async Task<IReadOnlyDictionary<int, object>> GetObjectByIdBAsync(
                    IReadOnlyList<int> ids,
                    CancellationToken cancellationToken)
                {
                    return await Task.FromResult(new Dictionary<int, object>());
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut;

            namespace TestNamespace;

            internal static class TestADataLoaderAttr
            {
                [DataLoader]
                public static async Task<IReadOnlyDictionary<int, object>> GetObjectByIdAAsync(
                    IReadOnlyList<int> ids,
                    CancellationToken cancellationToken)
                {
                    return await Task.FromResult(new Dictionary<int, object>());
                }
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_With_Problematic_Assembly_Name_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using HotChocolate.Types;

            namespace TestNamespace;

            internal class ATestBType: ObjectType<ATestB>;
            internal record ATestB(int Id);
            """
        ], assemblyName: "Custom-Module").MatchMarkdownAsync();
    }

    [Fact]
    public async Task DetectSourceSchemaDefaults_From_Shareable()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [Shareable]
            [QueryType]
            internal static partial class RootType
            {
                public static int Value => throw new System.NotImplementedException();
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task DetectSourceSchemaDefaults_From_Shareable_On_Field()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [QueryType]
            internal static partial class RootType
            {
                [Shareable]
                public static int Value => throw new System.NotImplementedException();
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task DetectSourceSchemaDefaults_From_Lookup()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using HotChocolate.Types;
            using HotChocolate.Types.Composite;

            namespace TestNamespace;

            [QueryType]
            internal static partial class RootType
            {
                [Lookup]
                public static int Value => throw new System.NotImplementedException();
            }
            """
        ]).MatchMarkdownAsync();
    }
}
