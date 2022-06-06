using System;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

[Flags]
public enum OutputModelKind
{
    Object = 1,
    Interface = 2,
    Fragment = 4,
    FragmentInterface = Fragment | Interface,
    FragmentObject = Fragment | Object
}
