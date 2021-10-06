using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;

namespace HotChocolate.Data
{
    public class AuthorFixture : InMemoryDatabaseTest
    {
        private static readonly Author[] _authors =
        {
            new() {Id = 1, Name = "Foo", Books = {new Book {Id = 1, Title = "Foo1"}}},
            new() {Id = 2, Name = "Bar", Books = {new Book {Id = 2, Title = "Bar1"}}},
            new() {Id = 3, Name = "Baz", Books = {new Book {Id = 3, Title = "Baz1"}}}
        };

        private static readonly SingleOrDefaultAuthor[] _singleOrDefaultAuthors = {new() {Id = 1, Name = "Foo"}};

        private readonly string _fileName;

        private readonly ISession _session;

        public AuthorFixture() : base(typeof(Author).Assembly)
        {
            _fileName = Guid.NewGuid().ToString("N") + ".db";
            _session = new ServiceCollection()
                .AddScoped(s => Session)
                .AddGraphQL()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<ISession>();

            List<Author> authors = new();
            authors.AddRange(_authors);
            foreach (Author author in authors) Session.Save(author);
        }

        public IQueryable<Author> Authors => _session.Query<Author>();
        public IQueryable<ZeroAuthor> ZeroAuthor => _session.Query<ZeroAuthor>();
        public IQueryable<SingleOrDefaultAuthor> SingleOrDefaultAuthor => _session.Query<SingleOrDefaultAuthor>();

        public void Dispose()
        {
            File.Delete(_fileName);
        }
    }
}
