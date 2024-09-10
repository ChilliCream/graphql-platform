namespace HotChocolate.Validation;

public class Dog(string name, string? nickname = null, int? barkVolume = null, bool barks = false)
    : IPet
{
    public string Name { get; set; } = name;

    public string? Nickname { get; set; } = nickname;

    public int? BarkVolume { get; set; } = barkVolume;

    public bool Barks { get; set; } = barks;

    public bool DoesKnowCommand(DogCommand dogCommand)
    {
        return true;
    }

    public bool IsHouseTrained(bool? atOtherHomes)
    {
        return true;
    }

    public Human? GetOwner()
    {
        return null;
    }
}
