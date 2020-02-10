using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Resolvers
{
    public class ResolverMetadata : IEquatable<ResolverMetadata>
    {
        public ResolverMetadata()
            : this(Array.Empty<string>(), Array.Empty<string>(), true)
        {
        }

        public ResolverMetadata(
            IReadOnlyList<string> dependsOn,
            IReadOnlyList<string> resources,
            bool isPure)
        {
            DependsOn = dependsOn;
            Resources = resources;
            IsPure = isPure;
        }

        public IReadOnlyList<string> DependsOn { get; }

        public IReadOnlyList<string> Resources { get; }

        public bool IsPure { get; }

        public ResolverMetadata WithDependsOn(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            var dependsOn = new List<string>(DependsOn)
            {
                propertyInfo.Name
            };
            return new ResolverMetadata(dependsOn, Resources, false);
        }

        public ResolverMetadata AsNonPure()
        {
            return new ResolverMetadata(DependsOn, Resources, false);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ResolverMetadata);
        }

        public bool Equals(ResolverMetadata other)
        {
            return other != null &&
                   DependsOn.All(other.DependsOn.Contains) &&
                   Resources.All(other.Resources.Contains) &&
                   IsPure == other.IsPure;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 487579150)
                ^ (-1521134295 * DependsOn.Aggregate(
                    487579150, (current, x) => current ^ -1521134295 * x?.GetHashCode() ?? 0))
                ^ (-1521134295 * Resources.Aggregate(
                    487579150, (current, x) => current ^ -1521134295 * x?.GetHashCode() ?? 0))
                ^ (-1521134295 * IsPure.GetHashCode());
            }
        }

        public static readonly ResolverMetadata Default = new ResolverMetadata();
    }
}
