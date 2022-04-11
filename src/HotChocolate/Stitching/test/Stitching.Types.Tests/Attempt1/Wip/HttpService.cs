using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching.Types;

public class HttpService : IServiceReference
{
    public HttpService(string name, string baseUrl, IServiceFeature[] features)
    {
        Name = name;
        BaseAddress = new Uri(baseUrl);
        Features = features;
    }

    public string Name { get; }
    public Uri BaseAddress { get; }
    public IEnumerable<IServiceFeature> Features { get; }
}