using System.Collections.Generic;

namespace HotChocolate.Benchmark.Tests.Execution
{
    public interface ICharacter
    {
        string Id { get; }
        string Name { get; }
        IReadOnlyList<string> Friends { get; }
        IReadOnlyList<Episode> AppearsIn { get; }
        double Height { get; }
    }

}
