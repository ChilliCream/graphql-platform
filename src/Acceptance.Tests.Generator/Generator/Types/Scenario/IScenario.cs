using System.Collections.Generic;
using Generator.ClassGenerator;

namespace Generator
{
    internal interface IScenario
    {
        string Name { get; }
        IEnumerable<string> Usings { get; }
        string Namespace { get; }
        IEnumerable<Statement> Fields { get; }
        IEnumerable<Statement> Constructor { get; }
        Background Background { get; }
        IEnumerable<Test> Tests { get; }
        void CreateBackground();
    }
}
