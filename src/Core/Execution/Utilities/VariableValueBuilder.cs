using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    // http://facebook.github.io/graphql/draft/#sec-Coercing-Variable-Values
    internal sealed class VariableValueBuilder
    {
        private readonly ISchema _schema;
        private readonly OperationDefinitionNode _operation;

        public VariableValueBuilder(
            ISchema schema,
            OperationDefinitionNode operation)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            _operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
        }

        // <summary>
        /// Creates the concrete variable values
        /// from the variables passed in by the user
        /// and the defaults defined in the query.
        /// </summary>
        /// <param name="variableValues">
        /// The variable values that were passed in by the user.
        /// </param>
        /// <returns>
        /// Returns the coerced variable values converted
        /// to their .net counterparts.
        /// </returns>/
        public VariableCollection CreateValues(
            IReadOnlyDictionary<string, object> variableValues)
        {
            IReadOnlyDictionary<string, object> values =
                variableValues ?? new Dictionary<string, object>();
            Dictionary<string, object> coercedValues =
                new Dictionary<string, object>();

            foreach (VariableDefinitionNode variableDefinition in
                _operation.VariableDefinitions)
            {
                Variable variable = CreateVariable(variableDefinition);
                CoerceVariableValue(values, ref variable);
                coercedValues[variable.Name] = variable.ParseLiteral();
            }

            return new VariableCollection(coercedValues);
        }

        private Variable CreateVariable(
            VariableDefinitionNode variableDefinition)
        {
            string variableName = variableDefinition.Variable.Name.Value;
            IType variableType = GetType(variableDefinition.Type);

            if (variableType is IInputType type)
            {
                return new Variable(
                    variableName, type,
                    variableDefinition.DefaultValue);
            }

            throw new QueryException(new VariableError(
                $"The variable type ({variableType.ToString()}) " +
                "must be an input object type.",
                variableName));
        }

        private void CoerceVariableValue(
            IReadOnlyDictionary<string, object> variableValues,
            ref Variable variable)
        {
            IValueNode valueNode = null;
            if (variableValues.TryGetValue(variable.Name, out var value))
            {
                valueNode = (value is IValueNode v)
                    ? v
                    : variable.Type.ParseValue(value);
            }
            else
            {
                value = variable.DefaultValue ?? NullValueNode.Default;
            }

            CleanUpValue(variable.Type, valueNode);

            variable = variable.WithValue(valueNode);

            CheckForNullValueViolation(variable);
            CheckForInvalidValueType(variable);
        }

        private IValueNode CleanUpValue(IInputType type, IValueNode value)
        {
            if (ValueNeedsCleanUp(type, value))
            {



            }

            return value;
        }

        private IValueNode RebuildValue(IInputType type, IValueNode value)
        {
            if (type.IsEnumType() || type.IsScalarType())
            {
                return RebuildScalarValue(type, value);
            }

            if (type.IsListType()
                && type.ListType() is ListType lt
                && value is ListValueNode lv)
            {
                return RebuildListValue(lt, lv);
            }

            if (type.IsInputObjectType()
                && type.NamedType() is InputObjectType iot
                && value is ObjectValueNode ov)
            {
                return RebuildObjectValue(iot, ov);
            }

            return value;
        }

        private IValueNode RebuildScalarValue(IInputType type, IValueNode value)
        {
            if (type.IsEnumType() && value is StringValueNode s)
            {
                return new EnumValueNode(s.Value);
            }

            return value;
        }

        private ObjectValueNode RebuildObjectValue(
            InputObjectType type,
            ObjectValueNode objectValue)
        {
            var fields = objectValue.Fields.ToLookup(t => t.Name.Value);
            var processedFields = new List<ObjectFieldNode>();

            foreach (InputField fieldDefinition in type.Fields)
            {
                ObjectFieldNode field = fields[fieldDefinition.Name]
                    .FirstOrDefault();

                if (field == null
                    || (fieldDefinition.Type.IsNonNullType()
                        && field.Value.IsNull()))
                {
                    IValueNode defaultValue =
                        fieldDefinition.DefaultValue
                        ?? NullValueNode.Default;

                    field = new ObjectFieldNode(
                        fieldDefinition.Name,
                        defaultValue);
                }
                else
                {
                    field = RebuildObjectField(fieldDefinition, field);
                }

                processedFields.Add(field);
            }

            return new ObjectValueNode(processedFields);
        }

        private ObjectFieldNode RebuildObjectField(
            InputField fieldDefinition,
            ObjectFieldNode field)
        {
            if (fieldDefinition.Type.IsEnumType()
                && field.Value is StringValueNode s)
            {
                return new ObjectFieldNode(
                    field.Location,
                    field.Name,
                    new EnumValueNode(s.Value));
            }
            else if (fieldDefinition.Type.IsObjectType()
                || fieldDefinition.Type.IsListType())
            {
                return new ObjectFieldNode(
                    field.Location,
                    field.Name,
                    RebuildValue(fieldDefinition.Type, field.Value));
            }

            return field;
        }

        private ListValueNode RebuildListValue(
            ListType type,
            ListValueNode listValue)
        {
            if (type.ElementType.IsEnumType()
                && listValue.Items.Count > 0
                && listValue.Items[0] is StringValueNode)
            {
                IValueNode[] items = new IValueNode[listValue.Items.Count];

                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = listValue.Items[i];

                    if (items[i] is StringValueNode s)
                    {
                        items[i] = new EnumValueNode(s.Value);
                    }
                }

                return new ListValueNode(items);
            }

            if (type.ElementType.IsInputObjectType()
               && type.ElementType.NamedType() is InputObjectType iot)
            {
                IValueNode[] items = new IValueNode[listValue.Items.Count];

                for (int i = 0; i < items.Length; i++)
                {
                    if (listValue.Items[i].IsNull())
                    {
                        items[i] = NullValueNode.Default;
                    }
                    else
                    {
                        items[i] = RebuildValue(iot, listValue.Items[i]);
                    }
                }

                return new ListValueNode(items);
            }

            return listValue;
        }

        private bool ValueNeedsCleanUp(IInputType type, IValueNode value)
        {
            if (type.IsEnumType() && value is StringValueNode)
            {
                return true;
            }

            if (type.IsInputObjectType()
                && type.NamedType() is InputObjectType iot
                && value is ObjectValueNode ov)
            {
                return ObjectNeedsCleanUp(iot, ov);
            }

            if (type.IsListType() && value is ListValueNode listValue)
            {
                return ListNeedsCleanUp(type.ListType(), listValue);
            }

            return false;
        }

        private bool ObjectNeedsCleanUp(
            InputObjectType type,
            ObjectValueNode objectValue)
        {
            foreach (ObjectFieldNode field in objectValue.Fields)
            {
                if (type.Fields.TryGetField(
                    field.Name.Value,
                    out InputField fieldDefinition))
                {
                    if (fieldDefinition.Type.IsEnumType())
                    {
                        return field.Value is StringValueNode;
                    }
                    else if (!fieldDefinition.Type.IsScalarType())
                    {
                        return ValueNeedsCleanUp(
                            fieldDefinition.Type, field.Value);
                    }
                }
            }

            return false;
        }

        private bool ListNeedsCleanUp(
            ListType type,
            ListValueNode listValue)
        {
            if (type.ElementType.IsEnumType()
                && listValue.Items.Count > 0
                && listValue.Items[0] is StringValueNode)
            {
                return true;
            }

            if (type.ElementType.IsInputObjectType()
                && type.ElementType.NamedType() is InputObjectType iot)
            {
                for (int i = 0; i < listValue.Items.Count; i++)
                {
                    if (listValue.Items[i] is ObjectValueNode ov
                        && ObjectNeedsCleanUp(iot, ov))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CheckForNullValueViolation(Variable variable)
        {
            if (variable.Type.IsNonNullType() && IsNulValue(variable.Value))
            {
                throw new QueryException(new VariableError(
                    "The variable value cannot be null.",
                    variable.Name));
            }
        }

        private void CheckForInvalidValueType(Variable variable)
        {
            if (!variable.Type.IsInstanceOfType(variable.Value))
            {
                throw new QueryException(new VariableError(
                    "The variable value is not of the correct type.",
                    variable.Name));
            }
        }

        private IType GetType(ITypeNode typeNode)
        {
            if (typeNode is NonNullTypeNode nonNullType)
            {
                return new NonNullType(GetType(nonNullType.Type));
            }

            if (typeNode is ListTypeNode listType)
            {
                return new ListType(GetType(listType.Type));
            }

            if (typeNode is NamedTypeNode namedType)
            {
                return _schema.GetType<INamedType>(namedType.Name.Value);
            }

            throw new NotSupportedException(
                "The type node kind is not supported.");
        }

        private static bool IsNulValue(IValueNode valueNode)
        {
            return valueNode == null || valueNode is NullValueNode;
        }

        private class Variable
        {
            private object _parsedValue;

            public Variable(string name, IInputType type,
                IValueNode defaultValue)
                : this(name, type, defaultValue, defaultValue)
            {
            }

            public Variable(string name, IInputType type,
                IValueNode defaultValue, IValueNode value)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(
                        "Variables cannot have an empty name.",
                        nameof(name));
                }

                Name = name;
                Type = type
                    ?? throw new ArgumentNullException(nameof(type));
                DefaultValue = defaultValue;
                Value = value;
            }

            public string Name { get; }

            public IInputType Type { get; }

            public IValueNode DefaultValue { get; }

            public IValueNode Value { get; }

            public Variable WithValue(IValueNode value)
            {
                return new Variable(Name, Type, DefaultValue, value);
            }

            public object ParseLiteral()
            {
                try
                {
                    if (_parsedValue == null)
                    {
                        _parsedValue = Type.ParseLiteral(Value);
                    }
                    return _parsedValue;
                }
                catch (ArgumentException ex)
                {
                    throw new QueryException(new VariableError(
                        ex.Message, Name));
                }
            }
        }
    }
}
