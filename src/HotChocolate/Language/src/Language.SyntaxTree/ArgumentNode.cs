using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class ArgumentNode
        : ISyntaxNode
    {
        public ArgumentNode(string name, string value)
            : this(null, new NameNode(name), new StringValueNode(value))
        {
        }

        public ArgumentNode(string name, int value)
            : this(null, new NameNode(name), new IntValueNode(value))
        {
        }

        public ArgumentNode(string name, IValueNode value)
            : this(null, new NameNode(name), value)
        {
        }

        public ArgumentNode(NameNode name, IValueNode value)
            : this(null, name, value)
        {
        }

        public ArgumentNode(Location? location, NameNode name, IValueNode value)
        {
            Location = location;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.Argument;

        public Location? Location { get; }

        public NameNode Name { get; }

        public IValueNode Value { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;
            yield return Value;
        }

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString() => SyntaxPrinter.Print(this, true);

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <param name="indented">
        /// A value that indicates whether the GraphQL output should be formatted,
        /// which includes indenting nested GraphQL tokens, adding
        /// new lines, and adding white space between property names and values.
        /// </param>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public ArgumentNode WithLocation(Location? location)
        {
            return new ArgumentNode(location, Name, Value);
        }

        public ArgumentNode WithName(NameNode name)
        {
            return new ArgumentNode(Location, name, Value);
        }

        public ArgumentNode WithValue(IValueNode value)
        {
            return new ArgumentNode(Location, Name, value);
        }
    }
}
