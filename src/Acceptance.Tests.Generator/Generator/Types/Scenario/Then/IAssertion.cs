using Generator.ClassGenerator;

namespace Generator
{
    internal interface IAssertion
    {
        Block CreateBlock(Statement header);
    }
}
