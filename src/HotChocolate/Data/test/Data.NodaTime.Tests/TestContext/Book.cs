using System.ComponentModel.DataAnnotations;
using NodaTime;

namespace HotChocolate.Data.NodaTime.TestContext;

public sealed class Book
{
    public int Id { get; init; }

    [MaxLength(50)]
    public string Title { get; init; } = null!;

    public Author Author { get; init; } = null!;

    public LocalDate PublishedDate { get; init; }
}
