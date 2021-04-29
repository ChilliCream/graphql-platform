using System;

namespace HotChocolate.Data
{
    public class AuthorFixture : IDisposable
    {
        public Author[] Authors { get; } =
        {
            new() { Id = 1, Name = "Foo", Books = { new Book { Id = 1, Title = "Foo1" } } },
            new() { Id = 2, Name = "Bar", Books = { new Book { Id = 2, Title = "Bar1" } } },
            new()
            {
                Id = 3,
                Name = "Baz",
                Books =
                {
                    new Book { Id = 3, Title = "Baz1" },
                    new Book { Id = 4, Title = "Baz2" }
                }
            }
        };

        public void Dispose()
        {
        }
    }
}
