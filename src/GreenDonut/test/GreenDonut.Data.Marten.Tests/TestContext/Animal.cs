using System.ComponentModel.DataAnnotations;

namespace GreenDonut.Data.TestContext;

public abstract class Animal
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    public int OwnerId { get; set; }

    public Owner? Owner { get; set; }
}
