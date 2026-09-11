using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using System.Collections.Immutable;
using System.Text;
using CookieCrumble;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class QueryContextUnionProjectionTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 128)]
    [InlineData(true, 128)]
    public async Task QueryContext_Should_ProjectUnion_When_BatchMembersHaveOppositeConditions(
        bool attributes,
        int padding)
    {
        // arrange
        var log = new BatchProjectionLog();
        var builder = new ServiceCollection().AddSingleton(log).AddGraphQL().AddQueryContext();

        if (attributes)
        {
            builder.AddQueryType<BatchProjectionQuery>();
        }
        else
        {
            builder.AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("noop").Resolve("unused");
                d.Field("product").ResolveBatchWith(
                    typeof(BatchProjectionQuery).GetMethod(nameof(BatchProjectionQuery.GetProduct))!);
            });
        }

        var executor = await builder.BuildRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var document = new StringBuilder("query($id:Int!,$take:Boolean!,$skip:Boolean!,$excluded:Boolean!");

        for (var i = 0; i < padding; i++)
        {
            document.Append($",$p{i}:Boolean!");
        }

        document.Append(") {");

        for (var i = 0; i < padding; i++)
        {
            document.Append($" p{i}:noop @include(if:$p{i})");
        }

        document.Append(" product(id:$id) @skip(if:$skip) { id left @include(if:$take) right @skip(if:$take) excluded @include(if:$excluded) } }");
        var sets = new List<IReadOnlyDictionary<string, object?>>();

        for (var id = 1; id <= 3; id++)
        {
            var variables = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["take"] = id == 2,
                ["skip"] = id == 3,
                ["excluded"] = id == 3
            };

            for (var i = 0; i < padding; i++)
            {
                variables[$"p{i}"] = false;
            }

            sets.Add(variables);
        }

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument(document.ToString()).SetVariableValues(sets).Build(),
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        new Snapshot(postFix: $"{attributes}_{padding}")
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batch.Results[2], "Skipped set")
            .Add(log.Sizes, "Batch sizes")
            .Add(log.Projected, "Projected before completion")
            .MatchMarkdownSnapshot();
        Assert.Equal(new[] { 2 }, log.Sizes);
        Assert.Equal(new[] { "1:left:right:", "2:left:right:" }, log.Projected);
        Assert.Equal(new[] { true }, log.SelectionMatches);
    }

    public sealed class BatchProjectionLog
    {
        public List<int> Sizes { get; } = [];
        public List<string> Projected { get; } = [];
        public List<bool> SelectionMatches { get; } = [];
    }

    public sealed class BatchProjectionProduct
    {
        public int Id { get; set; }
        public string? Left { get; set; }
        public string? Right { get; set; }
        public string? Excluded { get; set; }
    }

    public class BatchProjectionQuery
    {
        public string GetNoop() => "unused";

        [BatchResolver]
        public List<BatchProjectionProduct> GetProduct(
            List<int> id,
            QueryContext<BatchProjectionProduct> query,
            ISelection selection,
            IResolverContext context,
            [Service] BatchProjectionLog log)
        {
            log.Sizes.Add(id.Count);
            var source = id.Select(i => new BatchProjectionProduct
            {
                Id = i,
                Left = "left",
                Right = "right",
                Excluded = "excluded"
            }).AsQueryable();
            var projected = source.With(query).ToList();
            log.Projected.AddRange(projected.Select(p => $"{p.Id}:{p.Left}:{p.Right}:{p.Excluded}"));
            var selected = source.Select(selection.AsSelector<BatchProjectionProduct>(context.IncludeConditionFlags));
            log.SelectionMatches.Add(selected.AsEnumerable()
                .Select(p => $"{p.Id}:{p.Left}:{p.Right}:{p.Excluded}").SequenceEqual(log.Projected));
            return projected;
        }
    }

    [Fact]
    public async Task AsSelector_With_Single_Union_Field_Projects_Data()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              inspectionDefinitions {
                trigger {
                  ... on FieldDateTimeInspectionTrigger {
                    fieldModelKey
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AsSelector_With_Single_Union_Field_Projects_Data_With_Paging()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              pagedInspectionDefinitions(first: 10) {
                nodes {
                  trigger {
                    ... on FieldDateTimeInspectionTrigger {
                      fieldModelKey
                    }
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AsSelector_With_Single_Union_Field_Projects_Data_With_Nested_Paging_And_DataLoader()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              pagedInspectionGroups(first: 10) {
                nodes {
                  id
                  definitions(first: 10) {
                    nodes {
                      trigger {
                        ... on FieldDateTimeInspectionTrigger {
                          fieldModelKey
                        }
                      }
                    }
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AsSelector_With_Nested_Object_List_Projects_Data()
    {
        var executor = await CreateExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              inspectionTemplates {
                fields {
                  key
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AsSelector_With_List_Of_Union_Field_Projects_Data()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<TenantQuery>()
            .AddType<Entry>()
            .AddType<FileEntry>()
            .AddType<FolderEntry>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var result = await executor.ExecuteAsync(
            """
            {
              tenants {
                name
                entries {
                  ... on FileEntry {
                    fileName
                  }
                  ... on FolderEntry {
                    folderName
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        result.MatchSnapshot();
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync()
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension(typeof(InspectionGroupExtensions))
            .AddType<IInspectionTrigger>()
            .AddType<FieldDateTimeInspectionTrigger>()
            .AddDataLoader<InspectionDefinitionsByGroupDataLoader>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

    public class Query
    {
        public IQueryable<InspectionDefinition> GetInspectionDefinitions(ISelection selection)
            => s_singleData.AsQueryable()
                .Select(selection.AsSelector<InspectionDefinition>());

        [UsePaging]
        public IQueryable<InspectionDefinition> GetPagedInspectionDefinitions(ISelection selection)
            => s_singleData.AsQueryable()
                .Select(selection.AsSelector<InspectionDefinition>());

        [UsePaging]
        public IQueryable<InspectionGroup> GetPagedInspectionGroups(ISelection selection)
            => s_groupData.AsQueryable()
                .Select(selection.AsSelector<InspectionGroup>());

        public IQueryable<InspectionTemplate> GetInspectionTemplates(ISelection selection)
            => s_templateData.AsQueryable()
                .Select(selection.AsSelector<InspectionTemplate>());
    }

    public class InspectionDefinition
    {
        public Guid Id { get; set; }

        public int GroupId { get; set; }

        public required IInspectionTrigger Trigger { get; set; }
    }

    public class InspectionGroup
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public IReadOnlyList<InspectionDefinition> Definitions { get; set; } = [];
    }

    public class InspectionTemplate
    {
        public int Id { get; set; }

        public IReadOnlyList<InspectionTemplateField> Fields { get; set; } = [];
    }

    public class InspectionTemplateField
    {
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;
    }

    [ExtendObjectType<InspectionGroup>]
    public static class InspectionGroupExtensions
    {
        [BindMember(nameof(InspectionGroup.Definitions))]
        [UsePaging]
        public static async Task<Connection<InspectionDefinition>> GetDefinitionsAsync(
            [Parent("Id")] InspectionGroup group,
            PagingArguments pagingArgs,
            InspectionDefinitionsByGroupDataLoader dataLoader,
            ISelection selection,
            CancellationToken cancellationToken)
            => await dataLoader
                .With(pagingArgs)
                .Select(selection)
                .LoadAsync(group.Id, cancellationToken)
                .ToConnectionAsync();
    }

    public sealed class InspectionDefinitionsByGroupDataLoader
        : StatefulBatchDataLoader<int, Page<InspectionDefinition>>
    {
        public InspectionDefinitionsByGroupDataLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions options)
            : base(batchScheduler, options)
        {
        }

        protected override Task<IReadOnlyDictionary<int, Page<InspectionDefinition>>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Page<InspectionDefinition>> context,
            CancellationToken cancellationToken)
        {
            var pagingArgs = context.GetPagingArguments();
            var query = context.GetQueryContext<Page<InspectionDefinition>, InspectionDefinition>();
            var pageSize = pagingArgs.First ?? pagingArgs.Last ?? int.MaxValue;

            var grouped = s_groupDefinitionData
                .Where(x => keys.Contains(x.GroupId))
                .GroupBy(x => x.GroupId)
                .ToDictionary(x => x.Key, x => x.ToArray());

            var map = new Dictionary<int, Page<InspectionDefinition>>(keys.Count);

            foreach (var key in keys)
            {
                grouped.TryGetValue(key, out var sourceItems);
                sourceItems ??= [];

                var allItems = sourceItems
                    .AsQueryable()
                    .With(query, x => x.AddAscending(y => y.Id))
                    .ToArray();

                var take = Math.Min(pageSize, allItems.Length);
                var pageItems = allItems.Take(take).ToImmutableArray();
                var hasNext = allItems.Length > take;

                map[key] = Page<InspectionDefinition>.Create(
                    pageItems,
                    hasNextPage: hasNext,
                    hasPreviousPage: false,
                    createCursor: _ => string.Empty,
                    totalCount: allItems.Length);
            }

            return Task.FromResult<IReadOnlyDictionary<int, Page<InspectionDefinition>>>(map);
        }
    }

    [UnionType]
    public interface IInspectionTrigger;

    [ObjectType]
    public class FieldDateTimeInspectionTrigger : IInspectionTrigger
    {
        public required string FieldModelKey { get; set; }
    }

    private static readonly InspectionDefinition[] s_singleData =
    [
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            GroupId = 1,
            Trigger = new FieldDateTimeInspectionTrigger
            {
                FieldModelKey = "field-1"
            }
        }
    ];

    private static readonly InspectionGroup[] s_groupData =
    [
        new()
        {
            Id = 1,
            Name = "group-1"
        }
    ];

    private static readonly InspectionDefinition[] s_groupDefinitionData =
    [
        new()
        {
            Id = Guid.Parse("21111111-1111-1111-1111-111111111111"),
            GroupId = 1,
            Trigger = new FieldDateTimeInspectionTrigger
            {
                FieldModelKey = "field-1"
            }
        },
        new()
        {
            Id = Guid.Parse("31111111-1111-1111-1111-111111111111"),
            GroupId = 1,
            Trigger = new FieldDateTimeInspectionTrigger
            {
                FieldModelKey = "field-2"
            }
        }
    ];

    private static readonly InspectionTemplate[] s_templateData =
    [
        new()
        {
            Id = 1,
            Fields =
            [
                new()
                {
                    Key = "field-1",
                    Label = "Field 1"
                },
                new()
                {
                    Key = "field-2",
                    Label = "Field 2"
                }
            ]
        }
    ];

    // --- List union projection types ---

    public class TenantQuery
    {
        public IQueryable<Tenant> GetTenants(ISelection selection)
            => s_tenantData.AsQueryable()
                .Select(selection.AsSelector<Tenant>());
    }

    public class Tenant
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Entry> Entries { get; set; } = [];
    }

    [UnionType]
    public abstract class Entry
    {
        public int Id { get; set; }
    }

    [ObjectType]
    public class FileEntry : Entry
    {
        public required string FileName { get; set; }
    }

    [ObjectType]
    public class FolderEntry : Entry
    {
        public required string FolderName { get; set; }
    }

    private static readonly Tenant[] s_tenantData =
    [
        new()
        {
            Id = 1,
            Name = "tenant-1",
            Entries =
            [
                new FileEntry { Id = 1, FileName = "file-1.txt" },
                new FolderEntry { Id = 2, FolderName = "folder-1" },
                new FileEntry { Id = 3, FileName = "file-2.txt" }
            ]
        }
    ];
}
