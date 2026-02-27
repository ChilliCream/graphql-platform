using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public sealed class Issue8252ProbeTests
{
    [Fact]
    public async Task Union_Subset_With_OffsetPaging_Should_Project_Text_Items()
    {
        // arrange
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"issue8252-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";

        try
        {
            await using var services = new ServiceCollection()
                .AddDbContext<Issue8252Context>(b => b.UseSqlite(connectionString))
                .AddGraphQL()
                .AddQueryType<Issue8252Query>()
                .AddFiltering()
                .AddProjections()
                .AddUnionType<PostContent>()
                .AddType<TextContent>()
                .AddType<ImageContent>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<Issue8252Context>();
                await context.Database.EnsureCreatedAsync();

                context.Content.AddRange(
                    new TextContent
                    {
                        Discriminator = "TEXT",
                        Id = Guid.NewGuid(),
                        Text = "Hello World"
                    },
                    new ImageContent
                    {
                        Discriminator = "IMAGE",
                        Id = Guid.NewGuid(),
                        ImageUrl = "http://someurl",
                        Height = 10
                    });

                await context.SaveChangesAsync();
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync();

            // act
            var allFragments = await executor.ExecuteAsync(
                """
                {
                  contents {
                    items {
                      ... on ImageContent {
                        imageUrl
                      }
                      ... on TextContent {
                        text
                      }
                    }
                  }
                }
                """);

            var textOnly = await executor.ExecuteAsync(
                """
                {
                  contents {
                    items {
                      ... on TextContent {
                        text
                      }
                    }
                  }
                }
                """);

            var textOnlyWithTypeName = await executor.ExecuteAsync(
                """
                {
                  contents {
                    items {
                      __typename
                      ... on TextContent {
                        text
                      }
                    }
                  }
                }
                """);

            // assert
            var allFragmentsResult = allFragments.ExpectOperationResult();
            Assert.Empty(allFragmentsResult.Errors ?? []);

            var textOnlyResult = textOnly.ExpectOperationResult();
            var textOnlyWithTypeNameResult = textOnlyWithTypeName.ExpectOperationResult();

            Assert.Empty(textOnlyResult.Errors ?? []);
            Assert.Empty(textOnlyWithTypeNameResult.Errors ?? []);

            Assert.True(HasTextItem(textOnlyResult.ToJson()), textOnlyResult.ToJson());
            Assert.True(
                HasTextItem(textOnlyWithTypeNameResult.ToJson()),
                textOnlyWithTypeNameResult.ToJson());
        }
        finally
        {
            if (System.IO.File.Exists(dbFile))
            {
                System.IO.File.Delete(dbFile);
            }
        }
    }

    private static bool HasTextItem(string resultJson)
    {
        using var document = JsonDocument.Parse(resultJson);
        var items = document.RootElement.GetProperty("data")
            .GetProperty("contents")
            .GetProperty("items");

        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Object
                && item.TryGetProperty("text", out var text)
                && text.ValueKind is JsonValueKind.String
                && text.GetString() == "Hello World")
            {
                return true;
            }
        }

        return false;
    }

    public sealed class Issue8252Query
    {
        [UseOffsetPaging]
        [UseProjection]
        [UseFiltering]
        public IQueryable<PostContent> GetContents(Issue8252Context database)
            => database.Content;
    }

    public class PostContent
    {
        public Guid Id { get; set; }

        public required string Discriminator { get; set; }
    }

    public sealed class TextContent : PostContent
    {
        public required string Text { get; set; }
    }

    public sealed class ImageContent : PostContent
    {
        public required string ImageUrl { get; set; }

        public int Height { get; set; }
    }

    public sealed class Issue8252Context(DbContextOptions<Issue8252Context> options) : DbContext(options)
    {
        public DbSet<PostContent> Content => Set<PostContent>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<PostContent>().HasKey(e => e.Id);
            builder.Entity<PostContent>()
                .HasDiscriminator(e => e.Discriminator)
                .HasValue<TextContent>("TEXT")
                .HasValue<ImageContent>("IMAGE");
        }
    }
}
