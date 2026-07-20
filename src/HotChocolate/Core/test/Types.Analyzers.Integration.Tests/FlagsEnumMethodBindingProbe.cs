using SystemFlags = System.FlagsAttribute;

namespace HotChocolate.Types;

[SystemFlags]
public enum AnimalKind
{
    Dog = 1,
    Cat = 2
}

[Flags]
public enum FauxFlagsKind
{
    First = 1,
    Second = 2
}

[AttributeUsage(AttributeTargets.Enum)]
public sealed class FlagsAttribute : Attribute;

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
    public static AnimalKind PropertyAnimals
        => AnimalKind.Dog | AnimalKind.Cat;

    public static AnimalKind GetAnimals([Parent] Zoo zoo)
        => AnimalKind.Dog | AnimalKind.Cat;

    public static AnimalKind? GetNullableAnimals([Parent] Zoo zoo)
        => null;

    public static Task<AnimalKind> GetTaskAnimalsAsync([Parent] Zoo zoo)
        => Task.FromResult(AnimalKind.Dog | AnimalKind.Cat);

    public static ValueTask<AnimalKind> GetValueTaskAnimalsAsync([Parent] Zoo zoo)
        => ValueTask.FromResult(AnimalKind.Dog | AnimalKind.Cat);

    public static AnimalKind?[]? GetAnimalArray([Parent] Zoo zoo)
        => [AnimalKind.Dog | AnimalKind.Cat, null];

    public static List<AnimalKind> GetAnimalList([Parent] Zoo zoo)
        => [AnimalKind.Dog | AnimalKind.Cat];

    public static List<List<AnimalKind>> GetNestedAnimalList([Parent] Zoo zoo)
        => [[AnimalKind.Dog | AnimalKind.Cat]];

    public static Task<List<AnimalKind>> GetTaskAnimalListAsync([Parent] Zoo zoo)
        => Task.FromResult<List<AnimalKind>>([AnimalKind.Dog | AnimalKind.Cat]);

    [BatchResolver]
    public static ValueTask<List<AnimalKind>> GetBatchAnimalsAsync([Parent] List<Zoo> zoos)
        => ValueTask.FromResult<List<AnimalKind>>(
            zoos.Select(_ => AnimalKind.Dog | AnimalKind.Cat).ToList());

    public static FauxFlagsKind GetFauxFlags([Parent] Zoo zoo)
        => FauxFlagsKind.First;
}

public static partial class Query
{
    public static Animal GetAnimal()
        => new() { Id = "1", Kind = AnimalKind.Dog | AnimalKind.Cat };

    public static Zoo GetZoo()
        => new() { Id = "1" };

    public static string FormatAnimalKinds(AnimalKind kinds)
        => kinds.ToString();

    public static string FormatNullableAnimalKinds(AnimalKind? kinds)
        => kinds?.ToString() ?? "null";

    public static string FormatAnimalKindList(List<AnimalKind?> kinds)
        => string.Join(",", kinds.Select(t => t?.ToString() ?? "null"));
}
