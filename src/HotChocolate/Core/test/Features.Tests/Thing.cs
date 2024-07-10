// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

namespace HotChocolate.Features;

public class Thing : IThing
{
    public string Hello()
    {
        return "World";
    }
}
