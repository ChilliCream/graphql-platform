using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Mediator;

namespace Mocha.EntityFrameworkCore.Tests;

public sealed class EntityFrameworkTransactionMiddlewareTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly TestDbContext _dbContext;

    public EntityFrameworkTransactionMiddlewareTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddDbContext<TestDbContext>(o => o.UseSqlite(_connection));

        var builder = services.AddMediator()
            .UseEntityFrameworkTransactions<TestDbContext>();

        services.AddTransient<ICommandHandler<CreateItemCommand>, CreateItemHandler>();
        services.AddTransient<ICommandHandler<CreateItemWithResponseCommand, int>, CreateItemWithResponseHandler>();

        // Register pipelines (normally done by source-generated code)
        builder.ConfigureMediator(b =>
        {
            b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(CreateItemCommand),
                Terminal = PipelineBuilder.BuildVoidCommandTerminal<CreateItemCommand>()
            });
            b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(CreateItemWithResponseCommand),
                ResponseType = typeof(int),
                Terminal = PipelineBuilder.BuildCommandTerminal<CreateItemWithResponseCommand, int>()
            });
        });

        _provider = services.BuildServiceProvider();

        _dbContext = _provider.GetRequiredService<TestDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void UseEntityFrameworkTransactions_Registers_Runtime()
    {
        // Assert that the MediatorRuntime is available and has compiled pipelines
        var runtime = _provider.GetRequiredService<MediatorRuntime>();

        Assert.NotNull(runtime);
        Assert.NotNull(runtime.GetPipeline(typeof(CreateItemCommand)));
    }

    [Fact]
    public async Task Commits_Transaction_On_Success()
    {
        var runtime = _provider.GetRequiredService<MediatorRuntime>();
        using var scope = _provider.CreateScope();

        var context = runtime.RentContext();
        try
        {
            context.Services = scope.ServiceProvider;
            context.Message = new CreateItemCommand("Test Item");
            context.MessageType = typeof(CreateItemCommand);
            context.ResponseType = typeof(void);
            context.CancellationToken = CancellationToken.None;
            await runtime.GetPipeline(typeof(CreateItemCommand))(context);
        }
        finally
        {
            runtime.ReturnContext(context);
        }

        // Verify the item was persisted (transaction committed)
        var items = await _dbContext.Items.ToListAsync();
        Assert.Single(items);
        Assert.Equal("Test Item", items[0].Name);
    }

    [Fact]
    public async Task Rolls_Back_Transaction_On_Failure()
    {
        // Register a handler that saves to DB then throws
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseSqlite(_connection));
        var builder = services.AddMediator().UseEntityFrameworkTransactions<TestDbContext>();
        services.AddTransient<ICommandHandler<CreateItemCommand>, FailingCreateItemHandler>();

        builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
        {
            MessageType = typeof(CreateItemCommand),
            Terminal = PipelineBuilder.BuildVoidCommandTerminal<CreateItemCommand>()
        }));

        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<MediatorRuntime>();
        using var scope = provider.CreateScope();

        var context = runtime.RentContext();
        try
        {
            context.Services = scope.ServiceProvider;
            context.Message = new CreateItemCommand("Should Not Persist");
            context.MessageType = typeof(CreateItemCommand);
            context.ResponseType = typeof(void);
            context.CancellationToken = CancellationToken.None;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                runtime.GetPipeline(typeof(CreateItemCommand))(context).AsTask());
        }
        finally
        {
            runtime.ReturnContext(context);
        }

        // Verify the item was NOT persisted (transaction rolled back)
        var items = await _dbContext.Items.ToListAsync();
        Assert.Empty(items);
    }

    [Fact]
    public async Task Works_With_Response_Command()
    {
        var runtime = _provider.GetRequiredService<MediatorRuntime>();
        using var scope = _provider.CreateScope();

        var context = runtime.RentContext();
        int id;
        try
        {
            context.Services = scope.ServiceProvider;
            context.Message = new CreateItemWithResponseCommand("Response Item");
            context.MessageType = typeof(CreateItemWithResponseCommand);
            context.ResponseType = typeof(int);
            context.CancellationToken = CancellationToken.None;
            await runtime.GetPipeline(typeof(CreateItemWithResponseCommand))(context);
            id = (int)context.Result!;
        }
        finally
        {
            runtime.ReturnContext(context);
        }

        Assert.True(id > 0);
        var item = await _dbContext.Items.FindAsync(id);
        Assert.NotNull(item);
        Assert.Equal("Response Item", item.Name);
    }

    [Fact]
    public async Task Mediator_Dispatches_VoidCommand_Through_Pipeline()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new CreateItemCommand("Via Mediator"));

        var items = await _dbContext.Items.ToListAsync();
        Assert.Single(items);
        Assert.Equal("Via Mediator", items[0].Name);
    }

    [Fact]
    public async Task Mediator_Dispatches_ResponseCommand_Through_Pipeline()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var id = await mediator.SendAsync(new CreateItemWithResponseCommand("Via Mediator Response"));

        Assert.True(id > 0);
        var item = await _dbContext.Items.FindAsync(id);
        Assert.NotNull(item);
        Assert.Equal("Via Mediator Response", item.Name);
    }

    [Fact]
    public async Task Query_Skips_Transaction_By_Default()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseSqlite(_connection));
        var builder = services.AddMediator().UseEntityFrameworkTransactions<TestDbContext>();
        services.AddTransient<IQueryHandler<GetItemsQuery, List<TestItem>>, GetItemsHandler>();

        builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
        {
            MessageType = typeof(GetItemsQuery),
            ResponseType = typeof(List<TestItem>),
            Terminal = PipelineBuilder.BuildQueryTerminal<GetItemsQuery, List<TestItem>>()
        }));

        await using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        db.Database.EnsureCreated();
        db.Items.Add(new TestItem { Name = "Existing" });
        await db.SaveChangesAsync();

        var runtime = provider.GetRequiredService<MediatorRuntime>();
        var context = runtime.RentContext();
        try
        {
            context.Services = scope.ServiceProvider;
            context.Message = new GetItemsQuery();
            context.MessageType = typeof(GetItemsQuery);
            context.ResponseType = typeof(List<TestItem>);
            context.CancellationToken = CancellationToken.None;

            await runtime.GetPipeline(typeof(GetItemsQuery))(context);

            var result = (List<TestItem>)context.Result!;
            Assert.Single(result);

            // No transaction was opened - verify the database has no active transaction
            Assert.Null(db.Database.CurrentTransaction);
        }
        finally
        {
            runtime.ReturnContext(context);
        }
    }

    [Fact]
    public async Task ShouldCreateTransaction_Override_Enables_Query_Transactions()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseSqlite(_connection));
        var builder = services.AddMediator()
            .UseEntityFrameworkTransactions<TestDbContext>(options =>
            {
                // Force transactions for all message types including queries
                options.ShouldCreateTransaction = _ => true;
            });

        services.AddTransient<IQueryHandler<GetItemsQuery, List<TestItem>>, GetItemsHandler>();

        builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
        {
            MessageType = typeof(GetItemsQuery),
            ResponseType = typeof(List<TestItem>),
            Terminal = PipelineBuilder.BuildQueryTerminal<GetItemsQuery, List<TestItem>>()
        }));

        await using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        db.Database.EnsureCreated();

        var runtime = provider.GetRequiredService<MediatorRuntime>();
        var context = runtime.RentContext();
        try
        {
            context.Services = scope.ServiceProvider;
            context.Message = new GetItemsQuery();
            context.MessageType = typeof(GetItemsQuery);
            context.ResponseType = typeof(List<TestItem>);
            context.CancellationToken = CancellationToken.None;

            // Should not throw - transaction wrapping is enabled via delegate
            await runtime.GetPipeline(typeof(GetItemsQuery))(context);

            var result = (List<TestItem>)context.Result!;
            Assert.NotNull(result);
        }
        finally
        {
            runtime.ReturnContext(context);
        }
    }

    [Fact]
    public async Task CancelledToken_Should_ThrowOperationCanceled_When_TransactionBegins()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert: the cancelled token should propagate to BeginTransactionAsync
        // and result in an OperationCanceledException.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mediator.SendAsync(new CreateItemCommand("Should Not Persist"), cts.Token).AsTask());

        // Verify nothing was persisted
        var items = await _dbContext.Items.ToListAsync();
        Assert.Empty(items);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }
}

public record CreateItemCommand(string Name) : ICommand;

public record CreateItemWithResponseCommand(string Name) : ICommand<int>;

public class TestItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestItem> Items => Set<TestItem>();
}

public class CreateItemHandler(TestDbContext db) : ICommandHandler<CreateItemCommand>
{
    public async ValueTask HandleAsync(CreateItemCommand command, CancellationToken cancellationToken)
    {
        db.Items.Add(new TestItem { Name = command.Name });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class CreateItemWithResponseHandler(TestDbContext db) : ICommandHandler<CreateItemWithResponseCommand, int>
{
    public async ValueTask<int> HandleAsync(CreateItemWithResponseCommand command, CancellationToken cancellationToken)
    {
        var item = new TestItem { Name = command.Name };
        db.Items.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }
}

public class FailingCreateItemHandler(TestDbContext db) : ICommandHandler<CreateItemCommand>
{
    public async ValueTask HandleAsync(CreateItemCommand command, CancellationToken cancellationToken)
    {
        db.Items.Add(new TestItem { Name = command.Name });
        await db.SaveChangesAsync(cancellationToken);
        throw new InvalidOperationException("Simulated failure after save");
    }
}

public record GetItemsQuery : IQuery<List<TestItem>>;

public class GetItemsHandler(TestDbContext db) : IQueryHandler<GetItemsQuery, List<TestItem>>
{
    public async ValueTask<List<TestItem>> HandleAsync(GetItemsQuery query, CancellationToken cancellationToken)
        => await db.Items.ToListAsync(cancellationToken);
}
