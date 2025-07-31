using System.ComponentModel.DataAnnotations;

namespace GreenDonut.Data.TestContext;

public class Foo
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = default!;

    public int? BarId { get; set; }

    public Bar? Bar { get; set; }
}
