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

            InputField[] fields = config.Fields?.ToArray()
                ?? Array.Empty<InputField>();

            if (fields.Length == 0)
            {
                throw new ArgumentException(
                   $"The input object `{config.Name}` must at least " +
                   "provide one field.",
                   nameof(config));
            }

            foreach (InputField field in fields)
            {
                if (_fieldMap.ContainsKey(field.Name))
                {
                    throw new ArgumentException(
                        $"The input field name `{field.Name}` " +
                        $"is not unique within `{config.Name}`.",
                        nameof(config));
                }
                else
                {
                    _fieldMap.Add(field.Name, field);
                }
            }

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
            foreach (InputField field in _fieldMap.Values)
            {
                field.CompleteInitialization(reportError, this);
            }
        }

        #endregion
    }

    public class InputObjectTypeConfig
        : INamedTypeConfig
    {
        public InputObjectTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<InputField> Fields { get; set; }
    }
}
