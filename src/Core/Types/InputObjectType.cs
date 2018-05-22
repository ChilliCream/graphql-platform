using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputObjectType
        : INamedType
        , IInputType
        , INullableType
        , ITypeSystemNode
        , ITypeInitializer
    {
        private readonly IEnumerable<InputField> _fields;
        public readonly Dictionary<string, InputField> _fieldMap =
            new Dictionary<string, InputField>();

        public InputObjectType(InputObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An input object type name must not be null or empty.",
                    nameof(config));
            }

            if (config.Fields == null)
            {
                throw new ArgumentException(
                    "An input object must provide fields.",
                    nameof(config));
            }

            _fields = config.Fields;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public InputObjectTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, InputField> Fields { get; }

        // TODO : provide native type resolver with config.
        public Type NativeType => throw new NotImplementedException();

        public bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public object ParseLiteral(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            return Fields.Values;
        }

        #endregion

        #region Initialization

        void ITypeInitializer.CompleteInitialization(Action<SchemaError> reportError)
        {
            InputField[] fields = _fields.ToArray();
            if (fields.Length == 0)
            {
                reportError(new SchemaError(
                    $"The input type {Name} has no fields.",
                    this));
            }

            foreach (InputField field in fields)
            {
                field.CompleteInitialization(reportError, this);

                if (_fieldMap.ContainsKey(field.Name))
                {
                    reportError(new SchemaError(
                        $"The field name of field {field.Name} " +
                        $"is not unique within {Name}.",
                        this));
                }
                else
                {
                    _fieldMap.Add(field.Name, field);
                }
            }
        }

        #endregion

    }

    public class InputObjectTypeConfig
        : INamedTypeConfig
    {
        public InputObjectTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<InputField> Fields { get; }
    }
}
