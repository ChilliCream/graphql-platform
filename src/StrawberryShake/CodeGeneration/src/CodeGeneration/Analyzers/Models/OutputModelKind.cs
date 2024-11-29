namespace StrawberryShake.CodeGeneration.Analyzers.Models;

[Flags]
public enum OutputModelKind
{
    Object = 0,
    Interface = 1,
    Fragment = 2,
    FragmentInterface = Fragment | Interface,
    FragmentObject = Fragment | Object,
}
