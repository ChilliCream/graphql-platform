using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public sealed class JsonVariableValues
        : IVariableValues
    {
        private readonly ObjectValueNode _values;

        public JsonVariableValues(ObjectValueNode values)
        {
            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public IReadOnlyDictionary<string, object?> ToDictionary(
            IReadOnlyDictionary<string, IInputType> variableDefinitions)
        {
            var values = new Dictionary<string, object?>();

            for (var i = 0; i < _values.Fields.Count; i++)
            {
                ObjectFieldNode field = _values.Fields[i];
                IValueNode value = field.Value;

                if (variableDefinitions.TryGetValue(field.Name.Value, out IInputType? type))
                {
                     value = Rewrite(type, value);
                }

                values[field.Name.Value] = value;
            }

            return values;
        }

        private IValueNode Rewrite(
            IType inputType,
            IValueNode node)
        {
            switch (node)
            {
                case ObjectValueNode ov:
                    return Rewrite(inputType, ov);

                case ListValueNode lv:
                    return Rewrite(inputType, lv);

                case StringValueNode sv:
                    return inputType.Kind == TypeKind.Enum
                        ? new EnumValueNode(sv.Location, sv.Value)
                        : node;

                default:
                    return node;
            }
        }

        private ObjectValueNode Rewrite(
            IType inputType,
            ObjectValueNode node)
        {
            if (!(inputType.NamedType() is InputObjectType inputObjectType))
            {
                return node;
            }

            List<ObjectFieldNode>? fields = null;

            for (var i = 0; i < node.Fields.Count; i++)
            {
                ObjectFieldNode current = node.Fields[i];

                if(!inputObjectType.Fields.TryGetField(current.Name.Value, out IInputField? field))
                {
                    continue;
                }

                IValueNode value = Rewrite(field.Type, current.Value);

                if (fields is not null)
                {
                    fields.Add(current);
                }
                else if (!ReferenceEquals(current.Value, value))
                {
                    fields = new List<ObjectFieldNode>();

                    for (var j = 0; j < i; j++)
                    {
                        fields.Add(node.Fields[j]);
                    }

                    fields.Add(current.WithValue(value));
                }
            }

            return fields is not null ? node.WithFields(fields) : node;
        }

        private ListValueNode Rewrite(IType inputType, ListValueNode node)
        {
            if (!inputType.IsListType())
            {
                return node;
            }

            IType elementType = inputType.ListType().ElementType;
            List<IValueNode>? values = null;

            for (var i = 0; i < node.Items.Count; i++)
            {
                IValueNode current = node.Items[i];
                IValueNode value = Rewrite(elementType, current);

                if (values is not null)
                {
                    values.Add(current);
                }
                else if (!ReferenceEquals(current.Value, value))
                {
                    values = new List<IValueNode>();

                    for (var j = 0; j < i; j++)
                    {
                        values.Add(node.Items[j]);
                    }

                    values.Add(value);
                }
            }

            return values is not null ? node.WithItems(values) : node;
        }
    }
}
