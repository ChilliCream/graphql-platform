namespace HotChocolate.Validation;

public class Cat(string name, string? nickname = null, int? meowVolume = null) : IPet
{
    public string Name { get; set; } = name;

    public string? Nickname { get; set; } = nickname;

    public int? MeowVolume { get; set; } = meowVolume;

    public bool DoesKnowCommand(CatCommand catCommand)
    {
        return true;
    }
}
