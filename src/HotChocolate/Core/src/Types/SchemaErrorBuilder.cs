using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate
{
    public partial class SchemaErrorBuilder
        : ISchemaErrorBuilder
    {
        private Error _error = new Error();

        public ISchemaErrorBuilder SetMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    TypeResources.SchemaErrorBuilder_MessageIsNull,
                    nameof(message));
            }

            _error.Message = message;
            return this;
        }

        public ISchemaErrorBuilder SetCode(string code)
        {
            _error.Code = code;
            return this;
        }

        public ISchemaErrorBuilder SetPath(IReadOnlyCollection<object> path)
        {
            _error.Path = path;
            return this;
        }

        public ISchemaErrorBuilder SetPath(Path path)
        {
            _error.Path = path.ToList();
            return this;
        }

        public ISchemaErrorBuilder SetTypeSystemObject(
           ITypeSystemObject typeSystemObject)
        {
            _error.TypeSystemObject = typeSystemObject;
            return this;
        }

        public ISchemaErrorBuilder AddSyntaxNode(ISyntaxNode syntaxNode)
        {
            if (syntaxNode != null)
            {
                _error.SyntaxNodes = _error.SyntaxNodes.Add(syntaxNode);
            }
            return this;
        }

        public ISchemaErrorBuilder SetExtension(string key, object value)
        {
            _error.Extensions = _error.Extensions.SetItem(key, value);
            return this;
        }

        public ISchemaErrorBuilder SetException(Exception exception)
        {
            _error.Exception = exception;
            if (_error.Message is null)
            {
                _error.Message = exception.Message;
            }
            return this;
        }

        public ISchemaError Build()
        {
            if (_error.Message is null)
            {
                throw new InvalidOperationException("The message is mandatory.");
            }

            return _error.Clone();
        }

        public static SchemaErrorBuilder New() => new SchemaErrorBuilder();
    }
}
