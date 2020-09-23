using System;

namespace HotChocolate
{
    public sealed class RootPathSegment : Path
    {
        private RootPathSegment()
        {
            Parent = null;
            Depth = 0;
            Name = default;
        }

        /// <inheritdoc />
        public override Path? Parent { get; }

        /// <inheritdoc />
        public override int Depth { get; }

        /// <summary>
        ///  Gets the name representing a field on a result map.
        /// </summary>
        public NameString Name { get; }

        /// <inheritdoc />
        public override IndexerPathSegment Append(int index) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public override NamePathSegment Append(NameString name) =>
            New(name);

        /// <inheritdoc />
        public override string Print() => "/";

        /// <inheritdoc />
        public override bool Equals(Path? other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return ReferenceEquals(other, this);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (Parent?.GetHashCode() ?? 0) * 3;
                hash ^= Depth.GetHashCode() * 7;
                hash ^= Name.GetHashCode() * 11;
                return hash;
            }
        }

        public static RootPathSegment Instance { get; } = new RootPathSegment();
    }
}
