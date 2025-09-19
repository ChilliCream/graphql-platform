using System.ComponentModel.DataAnnotations;

namespace HotChocolate.Data.NodaTime.TestContext;

public sealed class Author
{
    public int Id { get; init; }

    [MaxLength(50)]
    public string Name { get; init; } = null!;

    public DateOnly BirthDate { get; init; }
}
