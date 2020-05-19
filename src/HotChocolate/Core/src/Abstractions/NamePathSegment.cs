#nullable  enable

namespace HotChocolate
{
    public sealed class NamePathSegment : Path
    {
        internal NamePathSegment(Path? parent, NameString name)
        {
            Parent = parent;
            Depth = parent is null ? 0 : parent.Depth + 1;
            Name = name;
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
        public override string Print()
        {
            string parent = Parent is null ? string.Empty : Parent.Print();
            return $"{parent}/{Name}";
        }

        /// <inheritdoc />
        public override bool Equals(Path? other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (other is NamePathSegment name &&
                Depth.Equals(name.Depth) &&
                Name.Equals(name.Name))
            {
                if (Parent is null)
                {
                    return name.Parent is null;
                }

                if (name.Parent is null)
                {
                    return false;
                }

                return Parent.Equals(name.Parent);
            }

            return false;
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
    }
}
