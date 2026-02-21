using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class QueryContextUnionProjectionTests
{
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
            """);

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
            """);

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
            """);

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
            => SingleData.AsQueryable()
                .Select(selection.AsSelector<InspectionDefinition>());

        [UsePaging]
        public IQueryable<InspectionDefinition> GetPagedInspectionDefinitions(ISelection selection)
            => SingleData.AsQueryable()
                .Select(selection.AsSelector<InspectionDefinition>());

        [UsePaging]
        public IQueryable<InspectionGroup> GetPagedInspectionGroups(ISelection selection)
            => GroupData.AsQueryable()
                .Select(selection.AsSelector<InspectionGroup>());
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

            var grouped = GroupDefinitionData
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

                map[key] = new Page<InspectionDefinition>(
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

    private static readonly InspectionDefinition[] SingleData =
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

    private static readonly InspectionGroup[] GroupData =
    [
        new()
        {
            Id = 1,
            Name = "group-1"
        }
    ];

    private static readonly InspectionDefinition[] GroupDefinitionData =
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
}
