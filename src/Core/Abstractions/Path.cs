using System;
using System.Collections.Generic;

namespace HotChocolate
{
    public sealed class Path
        : IEquatable<Path>
    {
        private Path(Path parent, NameString name)
        {
            Parent = parent;
            Name = name;
            Index = -1;
            IsIndexer = false;
            Depth = parent == null ? 0 : parent.Depth + 1;
        }

        private Path(Path parent, NameString name, int index)
        {
            Parent = parent;
            Name = name;
            Index = index;
            IsIndexer = true;
            Depth = parent == null ? 0 : parent.Depth + 1;
        }

        public Path Parent { get; }
        public NameString Name { get; }
        public int Index { get; }
        public bool IsIndexer { get; }
        public int Depth { get; }

        public Path Append(int index)
        {
            return new Path(Parent, Name, index);
        }

        public Path Append(NameString name)
        {
            return new Path(this, name);
        }

        public bool Equals(Path other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return ((Parent == null && other.Parent == null) || other.Parent.Equals(Parent))
                && string.Equals(other.Name, Name, StringComparison.Ordinal)
                && other.Index.Equals(Index);
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
                hash = hash ^ (Name.GetHashCode() * 7);
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

        public IReadOnlyList<object> ToCollection()
        {
            var stack = new List<object>();
            Path current = this;

            while (current != null)
            {
                if (current.IsIndexer)
                {
                    stack.Insert(0, current.Index);
                }
                stack.Insert(0, current.Name);
                current = current.Parent;
            }

            return stack;
        }

        public static Path New(NameString name)
        {
            return new Path(null, name);
        }
    }
}
