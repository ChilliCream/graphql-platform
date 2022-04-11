using System.Collections.Generic;

namespace HotChocolate.Stitching.Types;

public interface IServiceFeature
{
    string Name { get; }
    bool Enabled { get; }
    IDictionary<string, object> Settings { get; }
}