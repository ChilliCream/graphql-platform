using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data
{
    public class AuthorFixture : IDisposable
    {
        private static readonly Author[] _authors =
        {
            new Author
            {
                Id = 1, Name = "Foo", Books = { new Book { Id = 1, Title = "Foo1" } }
            },
            new Author
            {
                Id = 2, Name = "Bar", Books = { new Book { Id = 2, Title = "Bar1" } }
            },
            new Author { Id = 3, Name = "Baz", Books = { new Book { Id = 3, Title = "Baz1" } } }
        };

        public AuthorFixture()
        {
            BookContext context = new ServiceCollection()
                .AddDbContext<BookContext>(
                    b => b.UseSqlite("Data Source=authorFixture.db"))
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
            Context = context;
        }

        public BookContext Context;

        public void Dispose()
        {
            File.Delete("authorFixture.db");
        }
    }
}
