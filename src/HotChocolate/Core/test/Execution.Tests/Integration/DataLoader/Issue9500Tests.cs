using GreenDonut;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.DataLoader;

public class Issue9500Tests
{
    [Fact]
    public async Task Composite_DataLoader_Result_Overflows_Selection_Buffer_When_Paging_Many_Nodes()
    {
        const int nodeCount = 100_000;

        var executor = await CreateExecutorAsync(
            c => c
                .AddQueryType<Issue9500Query>()
                .AddDataLoader<INoteDataLoader, NoteDataLoader>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true));

        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    $$"""
                    {
                      items(first: {{nodeCount}}) {
                        edges {
                          cursor
                          node {
                            id
                            note {
                              comment
                              dueDate
                              progress
                              assignee
                              status
                              priority
                              category
                              createdBy
                              updatedBy
                              title
                              summary
                              kind
                              owner
                              reviewer
                              milestone
                            }
                          }
                        }
                      }
                    }
                    """)
                .Build());

        Assert.Empty(result.ExpectOperationResult().Errors);
    }

    public class Issue9500Query
    {
        [UsePaging(DefaultPageSize = 100000, MaxPageSize = 100000)]
        public IEnumerable<Item> GetItems()
            => Enumerable.Range(0, 100_000).Select(i => new Item(i));
    }

    public class Item(int id)
    {
        public int Id { get; } = id;

        public Task<Note?> GetNoteAsync(
            INoteDataLoader dataLoader,
            CancellationToken cancellationToken)
            => dataLoader.LoadAsync(Id, cancellationToken);
    }

    public interface INoteDataLoader
        : IDataLoader<int, Note>;

    public class NoteDataLoader(
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : BatchDataLoader<int, Note>(batchScheduler, options), INoteDataLoader
    {
        protected override Task<IReadOnlyDictionary<int, Note>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            return LoadAsync(keys, cancellationToken);
        }

        private static async Task<IReadOnlyDictionary<int, Note>> LoadAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return keys.ToDictionary(
                key => key,
                key => new Note(
                    $"Comment {key}",
                    $"2026-04-{(key % 28) + 1:00}",
                    key % 100,
                    $"Assignee {key}",
                    key % 2 == 0 ? "Open" : "Closed",
                    $"P{key % 5}",
                    $"Category {key % 7}",
                    $"Creator {key % 11}",
                    $"Updater {key % 13}",
                    $"Title {key}",
                    $"Summary {key}",
                    $"Kind {key % 3}",
                    $"Owner {key % 17}",
                    $"Reviewer {key % 19}",
                    $"Milestone {key % 23}"));
        }
    }

    public record Note(
        string Comment,
        string DueDate,
        int Progress,
        string Assignee,
        string Status,
        string Priority,
        string Category,
        string CreatedBy,
        string UpdatedBy,
        string Title,
        string Summary,
        string Kind,
        string Owner,
        string Reviewer,
        string Milestone);
}
