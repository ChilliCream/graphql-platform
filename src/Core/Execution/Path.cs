using System;
using System.Collections.Immutable;

namespace HotChocolate.Execution
{
    public sealed class Path
        : IEquatable<Path>
    {
        private Path(Path parent, string name)
        {
            Parent = parent;
            Name = name;
            Index = -1;
            IsIndexer = false;
            Depth = parent == null ? 0 : parent.Depth + 1;
        }

        private Path(Path parent, string name, int index)
        {
            Parent = parent;
            Name = name;
            Index = index;
            IsIndexer = true;
            Depth = parent.Depth + 1;
        }

        public Path Parent { get; }
        public string Name { get; }
        public int Index { get; }
        public bool IsIndexer { get; }
        public int Depth { get; }

        internal Path Append(int index)
        {
            return new Path(this.Parent, this.Name, index);
        }

        internal Path Append(string name)
        {
            return new Path(this, name);
        }

        public bool Equals(Path other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return (other.Parent.Equals(Parent)
                && string.Equals(other.Name, Name, StringComparison.Ordinal)
                && other.Index.Equals(Index));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (obj is Path p)
            {
                return Equals(p);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (Parent?.GetHashCode() ?? 0) * 3;

                if (Name != null)
                {
                    hash = hash ^ (Name.GetHashCode() * 7);
                }

                hash = hash ^ (Index.GetHashCode() * 11);

                return hash;
            }
        }

        public override string ToString()
        {
            string path = (Parent == null)
                ? string.Empty
                : Parent.ToString();

            if (IsIndexer)
            {
                return $"{path}/{Name}[{Index}]";
            }
            else
            {
                return $"{path}/{Name}";
            }
        }

        internal static Path New(string name)
        {
            return new Path(null, name);
        }
    }
}
