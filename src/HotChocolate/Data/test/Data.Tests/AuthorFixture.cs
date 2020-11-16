using System;

namespace HotChocolate.Data
{
    public class AuthorFixture : IDisposable
    {
        public Author[] Authors { get; } =
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

        public void Dispose()
        {
        }
    }
}
