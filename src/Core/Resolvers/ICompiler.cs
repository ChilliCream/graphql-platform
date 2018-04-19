using System.Reflection;

namespace HotChocolate.Resolvers
{
    public interface ICompiler
    {
        Assembly Compile(string sourceText);
    }
}