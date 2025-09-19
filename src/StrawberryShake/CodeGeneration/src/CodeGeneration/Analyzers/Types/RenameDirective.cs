namespace StrawberryShake.CodeGeneration.Analyzers.Types;

public class RenameDirective
{
    public RenameDirective(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
