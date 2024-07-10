using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class AuthorFixture : IDisposable
{
    private readonly string _fileName;

    private static readonly Author[] _authors =
    [
        new Author
        {
            Id = 1, Name = "Foo", Books = { new Book { Id = 1, Title = "Foo1", }, },
        },
        new Author
        {
            Id = 2, Name = "Bar", Books = { new Book { Id = 2, Title = "Bar1", }, },
        },
        new Author { Id = 3, Name = "Baz", Books = { new Book { Id = 3, Title = "Baz1", }, }, },
    ];

    private static readonly SingleOrDefaultAuthor[] _singleOrDefaultAuthors =
    [
        new SingleOrDefaultAuthor { Id = 1, Name = "Foo", },
    ];

    public AuthorFixture()
    {
        _fileName = Guid.NewGuid().ToString("N") + ".db";
        var context = new ServiceCollection()
            .AddDbContext<BookContext>(
                b => b.UseSqlite("Data Source=" + _fileName))
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<Query>()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<BookContext>();
        context.Database.EnsureCreated();
        context.Authors.AddRange(_authors);
        context.SaveChanges();
        context.SingleOrDefaultAuthors.AddRange(_singleOrDefaultAuthors);
        context.SaveChanges();
        Context = context;
    }

    public BookContext Context;

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_fileName);
    }
}
