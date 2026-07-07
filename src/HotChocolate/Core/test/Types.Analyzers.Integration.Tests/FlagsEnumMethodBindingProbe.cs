namespace HotChocolate.Types;

[Flags]
public enum AnimalKind
{
    Dog = 1,
    Cat = 2
}

public class Animal
{
    public required string Id { get; set; }

    public required AnimalKind Kind { get; set; }
}

public class Zoo
{
    public required string Id { get; set; }
}

[ObjectType<Zoo>]
public static partial class ZooType
{
    public static AnimalKind GetAnimals([Parent] Zoo zoo)
        => AnimalKind.Dog | AnimalKind.Cat;
}

public static partial class Query
{
    public static Animal GetAnimal()
        => new() { Id = "1", Kind = AnimalKind.Dog | AnimalKind.Cat };

    public static Zoo GetZoo()
        => new() { Id = "1" };
}
