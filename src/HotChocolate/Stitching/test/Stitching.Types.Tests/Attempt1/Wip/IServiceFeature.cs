using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public interface IServiceFeature
{
    string Name { get; }
    bool Enabled { get; }
    IDictionary<string, object> Settings { get; }
}