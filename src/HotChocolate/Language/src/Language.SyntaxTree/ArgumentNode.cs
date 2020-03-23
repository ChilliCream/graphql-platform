﻿using System;
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

        public NodeKind Kind { get; } = NodeKind.Argument;

        public Location? Location { get; }

        public NameNode Name { get; }

        public IValueNode Value { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;
            yield return Value;
        }

        public override string ToString() => SyntaxPrinter.Print(this, true);

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
