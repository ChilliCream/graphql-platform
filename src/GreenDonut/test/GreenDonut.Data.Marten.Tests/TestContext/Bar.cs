using System.ComponentModel.DataAnnotations;

namespace GreenDonut.Data.TestContext;

public class Bar
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string SomeField1 { get; set; } = default!;

    [MaxLength(100)]
    public string? SomeField2 { get; set; }
}
