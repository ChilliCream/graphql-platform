using System.ComponentModel.DataAnnotations;

namespace GreenDonut.Data.TestContext;

public class Owner
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    public List<Animal> Pets { get; set; } = new();
}
