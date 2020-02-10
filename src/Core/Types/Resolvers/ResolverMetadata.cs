using System;
using System.Collections.Generic;

namespace HotChocolate.Resolvers
{
    public class ResolverMetadata
    {
        public ResolverMetadata()
            : this(Array.Empty<string>(), Array.Empty<string>())
        {
        }

        public ResolverMetadata(
            IReadOnlyList<string> dependsOn,
            IReadOnlyList<string> resources)
        {
            DependsOn = dependsOn;
            Resources = resources;
        }

        public IReadOnlyList<string> DependsOn { get; }

        public IReadOnlyList<string> Resources { get; }

        public ResolverMetadata WithDependsOn(string dependency)
        {
            if (dependency == null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }

            var dependsOn = new List<string>(DependsOn);
            dependsOn.Add(dependency);

            return new ResolverMetadata(dependsOn, Resources);
        }
    }
}
