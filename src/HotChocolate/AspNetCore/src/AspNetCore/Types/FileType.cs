using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Types
{
    public class FileValueNode : IValueNode
    {
        public FileValueNode(object? value)
        {
            Value = value;
        }

        public object? Value { get; }

        // TODO : what would be the correct SyntaxKind?
        public SyntaxKind Kind => throw new NotImplementedException();

        public Language.Location? Location { get; }

        public bool Equals(IValueNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Value?.Equals(Value) == true;
        }

        public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public override string ToString() => ToString(true);
    }


    public class FileType : ScalarType<IFormFile>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileType"/> class.
        /// </summary>
        public FileType()
            : this(
                ScalarNames.File,
                // TODO : What should the description be?
                null,
                BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileType"/> class.
        /// </summary>
        public FileType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {

            return valueSyntax.Value;
        }

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            throw new NotImplementedException();
        }
    }
}