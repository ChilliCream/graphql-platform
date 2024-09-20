namespace HotChocolate.Validation;

public class Query
{
    public string? A { get; set; }

    public string? B { get; set; }

    public string? C { get; set; }

    public string? D { get; set; }

    public string? Y { get; set; }

    public Query? F1 { get; set; }

    public Query? F2 { get; set; }

    public Query? F3 { get; set; }

    public Query? GetField() => null;

    public Dog? GetDog()
    {
        return null;
    }

    public Dog? FindDog(ComplexInput? complex)
    {
        return null;
    }

    public Dog? FindDog2(ComplexInput2 complex)
    {
        return null;
    }

    public bool BooleanList(bool[]? booleanListArg)
    {
        return true;
    }

    public Human? GetHuman()
    {
        return null;
    }

    public Human? GetPet()
    {
        return null;
    }

    public object? GetCatOrDog()
    {
        return null;
    }

    public object? GetDogOrHuman()
    {
        return null;
    }

    public string[]? GetStringList()
    {
        return null;
    }

    public string? GetFieldWithArg(
        string? arg,
        string? arg1,
        string? arg2,
        string? arg3,
        string? arg4)
    {
        return null;
    }
}
