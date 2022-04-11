using System.Collections.Generic;

namespace HotChocolate.Stitching.Types;

public class AllowBatchingFeature : IServiceFeature
{
    public AllowBatchingFeature(bool enabled = true, IDictionary<string, object>? settings = default)
    {
        Name = "AllowBatching";
        Enabled = enabled;
        Settings = settings ?? new Dictionary<string, object>();
    }

    public string Name { get; }
    public bool Enabled { get; }
    public IDictionary<string, object> Settings { get; }
}