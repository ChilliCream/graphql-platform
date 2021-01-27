using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Types
{
    // TODO : This is not fully implemented
    public class FileValueNode : IValueNode<IFormFile>
    {
        private IFormFile _value;

        public FileValueNode(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is IFormFile formFileValue)
            {
                _value = formFileValue;
            }
            else
            {
                throw new Exception("Value is not IFormFile");
            }
        }

        public IFormFile Value => _value;

        public SyntaxKind Kind => SyntaxKind.Argument;

        public Language.Location? Location => throw new NotImplementedException();

        object? IValueNode.Value => _value;

        public bool Equals(IValueNode? other)
        {
            if (other == null)
                return false;

            return other.Value != Value;
        }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            throw new NotImplementedException();
        }

        public string ToString(bool indented)
        {
            return _value?.Name ?? "";
        }
    }

    // TODO : This is not fully implemented
    public class FileType : ScalarType<IFormFile>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileType"/> class.
        /// </summary>
        public FileType()
            : this(
                ScalarNames.File,
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

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            throw new NotImplementedException();
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            return valueSyntax.Value;
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            return new FileValueNode(resultValue);
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            throw new NotImplementedException();
        }
    }
}